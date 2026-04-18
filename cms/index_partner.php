<?php
session_start();
require_once 'db.php';

// Kiểm tra role, nếu không phải OWNER thì đẩy về trang login
if (!isset($_SESSION['user_id']) || strtoupper($_SESSION['role']) !== 'OWNER') {
    header("Location: login.php");
    exit;
}

$user_id = $_SESSION['user_id'] ?? 0;
$username = $_SESSION['username'] ?? 'Chủ cửa hàng';

// Lấy danh sách POI của OWNER hiện tại
$db = new SQLiteDB();
$pdo = $db->getPDO();

// Hàm đồng bộ POI sang Firebase Realtime Database
function syncPoiToFirebase($pdo, $id, $action = 'put') {
    $url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/POI/" . $id . ".json";
    
    if ($action === 'delete') {
        $options = ["http" => ["method" => "DELETE"]];
    } else {
        $stmt = $pdo->prepare("SELECT * FROM POI WHERE Id = :id");
        $stmt->execute([':id' => $id]);
        $poi = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$poi) return;
        
        $imgKey = null;
        foreach ($poi as $k => $v) {
            if (strtolower($k) === 'imageurls') {
                $imgKey = $k;
                break;
            }
        }
        
        if ($imgKey && is_string($poi[$imgKey])) {
            $decoded = json_decode($poi[$imgKey], true);
            if (is_array($decoded)) {
                unset($poi[$imgKey]);
                $poi['imageUrls'] = $decoded;
            }
        }
        
        $options = [
            "http" => [
                "method" => "PUT",
                "header" => "Content-Type: application/json",
                "content" => json_encode($poi)
            ]
        ];
    }
    
    $context = stream_context_create($options);
    @file_get_contents($url, false, $context);
}

// Hàm đồng bộ POIImage sang Firebase Realtime Database
function syncPoiImageToFirebase($pdo, $id, $action = 'put') {
    if (empty($id)) return; // Bảo vệ: không xóa hoặc ghi đè node gốc nếu ID rỗng
    $url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/POIImage/" . $id . ".json";
    
    if ($action === 'delete') {
        $options = ["http" => ["method" => "DELETE"]];
    } else {
        $stmt = $pdo->prepare("SELECT * FROM POIImage WHERE Id = :id OR id = :id_lower");
        $stmt->execute([':id' => $id, ':id_lower' => $id]);
        $poiImage = $stmt->fetch(PDO::FETCH_ASSOC);
        if (!$poiImage) return;
        
        $options = [
            "http" => [
                "method" => "PUT",
                "header" => "Content-Type: application/json",
                "content" => json_encode($poiImage)
            ]
        ];
    }
    
    $context = stream_context_create($options);
    @file_get_contents($url, false, $context);
}

// Hàm upload ảnh lên Cloudinary
function uploadToCloudinary($tmpFile) {
    if (!$tmpFile || !file_exists($tmpFile)) return null;
    $cloudName = '';
    $apiKey = '';
    $apiSecret = '';
    $timestamp = time();
    $signature = sha1("timestamp=" . $timestamp . $apiSecret);
    
    $ch = curl_init("https://api.cloudinary.com/v1_1/" . $cloudName . "/image/upload");
    curl_setopt($ch, CURLOPT_POST, true);
    $cfile = new CURLFile($tmpFile);
    $data = [
        'file' => $cfile,
        'api_key' => $apiKey,
        'timestamp' => $timestamp,
        'signature' => $signature
    ];
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    $response = curl_exec($ch);
    curl_close($ch);
    
    $result = json_decode($response, true);
    return $result['secure_url'] ?? null;
}

// Xử lý cập nhật POI (Chỉ cập nhật nếu POI thuộc về OWNER hiện tại)
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'update_poi') {
    try {
        $id = (int)$_POST['id'];
        
        // Xác minh POI có phải của owner này không
        $stmtVerify = $pdo->prepare("SELECT ownerId FROM POI WHERE Id = :id");
        $stmtVerify->execute([':id' => $id]);
        $verifyPoi = $stmtVerify->fetch(PDO::FETCH_ASSOC);
        
        if ($verifyPoi && $verifyPoi['ownerId'] == $user_id) {
            $name = trim($_POST['name'] ?? '');
            $latitude = (float)($_POST['latitude'] ?? 0);
            $longitude = (float)($_POST['longitude'] ?? 0);
            $address = trim($_POST['address'] ?? '');
            $phone = trim($_POST['phone'] ?? '');
            $descriptionText = trim($_POST['descriptionText'] ?? '');

            $hasEn = isset($_POST['has_en']) && $_POST['has_en'] == '1';
            $hasZh = isset($_POST['has_zh']) && $_POST['has_zh'] == '1';
            $hasJa = isset($_POST['has_ja']) && $_POST['has_ja'] == '1';
            $hasKo = isset($_POST['has_ko']) && $_POST['has_ko'] == '1';
            $hasFr = isset($_POST['has_fr']) && $_POST['has_fr'] == '1';
            $hasRu = isset($_POST['has_ru']) && $_POST['has_ru'] == '1';

            $descriptionEn = $hasEn ? trim($_POST['descriptionEn'] ?? '') : '';
            $descriptionZh = $hasZh ? trim($_POST['descriptionZh'] ?? '') : '';
            $descriptionJa = $hasJa ? trim($_POST['descriptionJa'] ?? '') : '';
            $descriptionKo = $hasKo ? trim($_POST['descriptionKo'] ?? '') : '';
            $descriptionFr = $hasFr ? trim($_POST['descriptionFr'] ?? '') : '';
            $descriptionRu = $hasRu ? trim($_POST['descriptionRu'] ?? '') : '';
            $ttsScript = trim($_POST['ttsScript'] ?? '');
            $ttsScriptEn = $hasEn ? trim($_POST['ttsScriptEn'] ?? '') : '';
            $ttsScriptZh = $hasZh ? trim($_POST['ttsScriptZh'] ?? '') : '';
            $ttsScriptJa = $hasJa ? trim($_POST['ttsScriptJa'] ?? '') : '';
            $ttsScriptKo = $hasKo ? trim($_POST['ttsScriptKo'] ?? '') : '';
            $ttsScriptFr = $hasFr ? trim($_POST['ttsScriptFr'] ?? '') : '';
            $ttsScriptRu = $hasRu ? trim($_POST['ttsScriptRu'] ?? '') : '';
            
            $imageUrls = trim($_POST['imageUrls'] ?? '');
            
            $uploadedAvatar = isset($_FILES['avatar']['tmp_name']) && $_FILES['avatar']['error'] === UPLOAD_ERR_OK ? uploadToCloudinary($_FILES['avatar']['tmp_name']) : null;
            $uploadedBanner = isset($_FILES['banner']['tmp_name']) && $_FILES['banner']['error'] === UPLOAD_ERR_OK ? uploadToCloudinary($_FILES['banner']['tmp_name']) : null;

            $currentImages = json_decode($imageUrls, true);
            if (!is_array($currentImages) || empty($currentImages)) $currentImages = ["", ""];
            if (count($currentImages) < 2) {
                while(count($currentImages) < 2) $currentImages[] = "";
            }
            if ($uploadedAvatar) $currentImages[0] = $uploadedAvatar;
            if ($uploadedBanner) $currentImages[1] = $uploadedBanner;
            
            $currentImages = array_values($currentImages);
            $imageUrlsToSave = json_encode($currentImages, JSON_UNESCAPED_SLASHES);
            
            if ($imageUrls !== $imageUrlsToSave) {
                $imageUrls = $imageUrlsToSave;
            }

            if ($currentImages[0] !== "" || $currentImages[1] !== "") {
                $stmtCheckAvatar = $pdo->prepare("SELECT * FROM POIImage WHERE POIId = :poiId AND ImageType = 'avatar'");
                $stmtCheckAvatar->execute([':poiId' => $id]);
                $rowAvatar = $stmtCheckAvatar->fetch(PDO::FETCH_ASSOC);
                
                if ($currentImages[0] !== "") {
                    if ($rowAvatar) {
                        $imgId = $rowAvatar['Id'] ?? $rowAvatar['id'] ?? null;
                        $pdo->prepare("UPDATE POIImage SET ImageUrl = :imageUrl WHERE POIId = :poiId AND ImageType = 'avatar'")->execute([':imageUrl' => $currentImages[0], ':poiId' => $id]);
                        syncPoiImageToFirebase($pdo, $imgId, 'put');
                    } else {
                        $stmtMaxImg = $pdo->query("SELECT MAX(Id) FROM POIImage");
                        $newImgId = (int)$stmtMaxImg->fetchColumn() + 1;
                        $pdo->prepare("INSERT INTO POIImage (Id, POIId, ImageUrl, ImageType, Caption, SortOrder) VALUES (:id, :poiId, :imageUrl, 'avatar', '', 0)")->execute([':id' => $newImgId, ':poiId' => $id, ':imageUrl' => $currentImages[0]]);
                        syncPoiImageToFirebase($pdo, $newImgId, 'put');
                    }
                }
                
                $stmtCheckBanner = $pdo->prepare("SELECT * FROM POIImage WHERE POIId = :poiId AND ImageType = 'banner'");
                $stmtCheckBanner->execute([':poiId' => $id]);
                $rowBanner = $stmtCheckBanner->fetch(PDO::FETCH_ASSOC);
                
                if ($currentImages[1] !== "") {
                    if ($rowBanner) {
                        $imgId = $rowBanner['Id'] ?? $rowBanner['id'] ?? null;
                        $pdo->prepare("UPDATE POIImage SET ImageUrl = :imageUrl WHERE POIId = :poiId AND ImageType = 'banner'")->execute([':imageUrl' => $currentImages[1], ':poiId' => $id]);
                        syncPoiImageToFirebase($pdo, $imgId, 'put');
                    } else {
                        $stmtMaxImg = $pdo->query("SELECT MAX(Id) FROM POIImage");
                        $newImgId = (int)$stmtMaxImg->fetchColumn() + 1;
                        $pdo->prepare("INSERT INTO POIImage (Id, POIId, ImageUrl, ImageType, Caption, SortOrder) VALUES (:id, :poiId, :imageUrl, 'banner', '', 0)")->execute([':id' => $newImgId, ':poiId' => $id, ':imageUrl' => $currentImages[1]]);
                        syncPoiImageToFirebase($pdo, $newImgId, 'put');
                    }
                }
            }

            $mapLink = trim($_POST['mapLink'] ?? '');
            $triggerRadiusMeters = (int)($_POST['triggerRadiusMeters'] ?? 20);
            
            // Bất kể trạng thái cũ là gì, khi Vendor lưu thay đổi thì đều chuyển về chờ duyệt (-1)
            $isActive = -1;

            $sql = "UPDATE POI SET 
                        Name = :name,
                        Latitude = :latitude,
                        Longitude = :longitude,
                        Address = :address,
                        Phone = :phone,
                        DescriptionText = :descriptionText,
                        DescriptionEn = :descriptionEn,
                        DescriptionZh = :descriptionZh,
                        DescriptionJa = :descriptionJa,
                        DescriptionKo = :descriptionKo,
                        DescriptionFr = :descriptionFr,
                        DescriptionRu = :descriptionRu,
                        TtsScript = :ttsScript,
                        TtsScriptEn = :ttsScriptEn,
                        TtsScriptZh = :ttsScriptZh,
                        TtsScriptJa = :ttsScriptJa,
                        TtsScriptKo = :ttsScriptKo,
                        TtsScriptFr = :ttsScriptFr,
                        TtsScriptRu = :ttsScriptRu,
                        ImageUrls = :imageUrls,
                        MapLink = :mapLink,
                        triggerRadiusMeters = :triggerRadiusMeters,
                        IsActive = :isActive
                    WHERE Id = :id AND ownerId = :owner_id";

            $stmtUpdate = $pdo->prepare($sql);
            $stmtUpdate->execute([
                ':name' => $name,
                ':latitude' => $latitude,
                ':longitude' => $longitude,
                ':address' => $address,
                ':phone' => $phone,
                ':descriptionText' => $descriptionText,
                ':descriptionEn' => $descriptionEn,
                ':descriptionZh' => $descriptionZh,
                ':descriptionJa' => $descriptionJa,
                ':descriptionKo' => $descriptionKo,
                ':descriptionFr' => $descriptionFr,
                ':descriptionRu' => $descriptionRu,
                ':ttsScript' => $ttsScript,
                ':ttsScriptEn' => $ttsScriptEn,
                ':ttsScriptZh' => $ttsScriptZh,
                ':ttsScriptJa' => $ttsScriptJa,
                ':ttsScriptKo' => $ttsScriptKo,
                ':ttsScriptFr' => $ttsScriptFr,
                ':ttsScriptRu' => $ttsScriptRu,
                ':imageUrls' => $imageUrls,
                ':mapLink' => $mapLink,
                ':triggerRadiusMeters' => $triggerRadiusMeters,
                ':isActive' => $isActive,
                ':id' => $id,
                ':owner_id' => $user_id
            ]);
            
            // Đồng bộ Firebase
            syncPoiToFirebase($pdo, $id, 'put');
        }
        
        header("Location: index_partner.php");
        exit;
    } catch (Exception $e) {
        $error_msg = $e->getMessage();
    }
}

// Xử lý thêm POI
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'add_poi') {
    try {
        $name = trim($_POST['name'] ?? '');
        $latitude = (float)($_POST['latitude'] ?? 0);
        $longitude = (float)($_POST['longitude'] ?? 0);
        $address = trim($_POST['address'] ?? '');
        $phone = trim($_POST['phone'] ?? '');
        $descriptionText = trim($_POST['descriptionText'] ?? '');
        
        $hasEn = isset($_POST['has_en']) && $_POST['has_en'] == '1';
        $hasZh = isset($_POST['has_zh']) && $_POST['has_zh'] == '1';
        $hasJa = isset($_POST['has_ja']) && $_POST['has_ja'] == '1';
        $hasKo = isset($_POST['has_ko']) && $_POST['has_ko'] == '1';
        $hasFr = isset($_POST['has_fr']) && $_POST['has_fr'] == '1';
        $hasRu = isset($_POST['has_ru']) && $_POST['has_ru'] == '1';
        
        $descriptionEn = $hasEn ? trim($_POST['descriptionEn'] ?? '') : '';
        $descriptionZh = $hasZh ? trim($_POST['descriptionZh'] ?? '') : '';
        $descriptionJa = $hasJa ? trim($_POST['descriptionJa'] ?? '') : '';
        $descriptionKo = $hasKo ? trim($_POST['descriptionKo'] ?? '') : '';
        $descriptionFr = $hasFr ? trim($_POST['descriptionFr'] ?? '') : '';
        $descriptionRu = $hasRu ? trim($_POST['descriptionRu'] ?? '') : '';
        $ttsScript = trim($_POST['ttsScript'] ?? '');
        $ttsScriptEn = $hasEn ? trim($_POST['ttsScriptEn'] ?? '') : '';
        $ttsScriptZh = $hasZh ? trim($_POST['ttsScriptZh'] ?? '') : '';
        $ttsScriptJa = $hasJa ? trim($_POST['ttsScriptJa'] ?? '') : '';
        $ttsScriptKo = $hasKo ? trim($_POST['ttsScriptKo'] ?? '') : '';
        $ttsScriptFr = $hasFr ? trim($_POST['ttsScriptFr'] ?? '') : '';
        $ttsScriptRu = $hasRu ? trim($_POST['ttsScriptRu'] ?? '') : '';
        
        $imageUrls = trim($_POST['imageUrls'] ?? '');
        
        $uploadedAvatar = isset($_FILES['avatar']['tmp_name']) && $_FILES['avatar']['error'] === UPLOAD_ERR_OK ? uploadToCloudinary($_FILES['avatar']['tmp_name']) : null;
        $uploadedBanner = isset($_FILES['banner']['tmp_name']) && $_FILES['banner']['error'] === UPLOAD_ERR_OK ? uploadToCloudinary($_FILES['banner']['tmp_name']) : null;

        $currentImages = json_decode($imageUrls, true);
        if (!is_array($currentImages) || empty($currentImages)) $currentImages = ["", ""];
        if (count($currentImages) < 2) {
            while(count($currentImages) < 2) $currentImages[] = "";
        }
        if ($uploadedAvatar) $currentImages[0] = $uploadedAvatar;
        if ($uploadedBanner) $currentImages[1] = $uploadedBanner;
        
        $currentImages = array_values($currentImages);
        $imageUrlsToSave = json_encode($currentImages, JSON_UNESCAPED_SLASHES);
        
        if ($imageUrls !== $imageUrlsToSave) {
            $imageUrls = $imageUrlsToSave;
        }

        $mapLink = trim($_POST['mapLink'] ?? '');
        $triggerRadiusMeters = (int)($_POST['triggerRadiusMeters'] ?? 20);
        $priority = 0; // Mặc định là 0 khi vendor tự thêm
        
        // POI do vendor tạo mặc định isActive = -1 (chờ duyệt)
        $isActive = -1;
        $createdAt = date('Y-m-d H:i:s');
        $updatedAt = date('Y-m-d H:i:s');

        // Lấy ID lớn nhất hiện tại để tự động tăng
        $stmtMax = $pdo->query("SELECT MAX(Id) FROM POI");
        $maxId = (int)$stmtMax->fetchColumn();
        $newId = $maxId + 1;
        
        $sql = "INSERT INTO POI (Id, Name, Latitude, Longitude, Address, Phone, DescriptionText, DescriptionEn, DescriptionZh, DescriptionJa, DescriptionKo, DescriptionFr, DescriptionRu, TtsScript, TtsScriptEn, TtsScriptZh, TtsScriptJa, TtsScriptKo, TtsScriptFr, TtsScriptRu, ImageUrls, MapLink, triggerRadiusMeters, IsActive, CreatedAt, UpdatedAt, ownerId, priority)
                VALUES (:id, :name, :latitude, :longitude, :address, :phone, :descriptionText, :descriptionEn, :descriptionZh, :descriptionJa, :descriptionKo, :descriptionFr, :descriptionRu, :ttsScript, :ttsScriptEn, :ttsScriptZh, :ttsScriptJa, :ttsScriptKo, :ttsScriptFr, :ttsScriptRu, :imageUrls, :mapLink, :triggerRadiusMeters, :isActive, :createdAt, :updatedAt, :owner_id, :priority)";

        $stmtAdd = $pdo->prepare($sql);
        $stmtAdd->execute([
            ':id' => $newId,
            ':name' => $name,
            ':latitude' => $latitude,
            ':longitude' => $longitude,
            ':address' => $address,
            ':phone' => $phone,
            ':descriptionText' => $descriptionText,
            ':descriptionEn' => $descriptionEn,
            ':descriptionZh' => $descriptionZh,
            ':descriptionJa' => $descriptionJa,
            ':descriptionKo' => $descriptionKo,
            ':descriptionFr' => $descriptionFr,
            ':descriptionRu' => $descriptionRu,
            ':ttsScript' => $ttsScript,
            ':ttsScriptEn' => $ttsScriptEn,
            ':ttsScriptZh' => $ttsScriptZh,
            ':ttsScriptJa' => $ttsScriptJa,
            ':ttsScriptKo' => $ttsScriptKo,
            ':ttsScriptFr' => $ttsScriptFr,
            ':ttsScriptRu' => $ttsScriptRu,
            ':imageUrls' => $imageUrls,
            ':mapLink' => $mapLink,
            ':triggerRadiusMeters' => $triggerRadiusMeters,
            ':isActive' => $isActive,
            ':createdAt' => $createdAt,
            ':updatedAt' => $updatedAt,
            ':owner_id' => $user_id,
            ':priority' => $priority
        ]);
        
        if ($currentImages[0] !== "" || $currentImages[1] !== "") {
            $stmtDel = $pdo->prepare("SELECT * FROM POIImage WHERE POIId = :poiId");
            $stmtDel->execute([':poiId' => $newId]);
            while ($rowDel = $stmtDel->fetch(PDO::FETCH_ASSOC)) {
                $imgId = $rowDel['Id'] ?? $rowDel['id'] ?? null;
                syncPoiImageToFirebase($pdo, $imgId, 'delete');
            }
            $pdo->prepare("DELETE FROM POIImage WHERE POIId = :poiId")->execute([':poiId' => $newId]);
            
            if ($currentImages[0] !== "") {
                $stmtMaxImg = $pdo->query("SELECT MAX(Id) FROM POIImage");
                $newImgId = (int)$stmtMaxImg->fetchColumn() + 1;
                $pdo->prepare("INSERT INTO POIImage (Id, POIId, ImageUrl, ImageType, Caption, SortOrder) VALUES (:id, :poiId, :imageUrl, 'avatar', '', 0)")->execute([':id' => $newImgId, ':poiId' => $newId, ':imageUrl' => $currentImages[0]]);
                syncPoiImageToFirebase($pdo, $newImgId, 'put');
            }
            
            if ($currentImages[1] !== "") {
                $stmtMaxImg = $pdo->query("SELECT MAX(Id) FROM POIImage");
                $newImgId = (int)$stmtMaxImg->fetchColumn() + 1;
                $pdo->prepare("INSERT INTO POIImage (Id, POIId, ImageUrl, ImageType, Caption, SortOrder) VALUES (:id, :poiId, :imageUrl, 'banner', '', 0)")->execute([':id' => $newImgId, ':poiId' => $newId, ':imageUrl' => $currentImages[1]]);
                syncPoiImageToFirebase($pdo, $newImgId, 'put');
            }
        }

        // Đồng bộ Firebase
        syncPoiToFirebase($pdo, $newId, 'put');
        
        header("Location: index_partner.php");
        exit;
    } catch (Exception $e) {
        $error_msg = "Lỗi thêm mới: " . $e->getMessage();
    }
}

// Xử lý xoá POI (Chỉ xoá nếu POI thuộc về OWNER hiện tại)
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'delete_poi') {
    try {
        $id = (int)$_POST['id'];
        
        $stmtVerify = $pdo->prepare("SELECT ownerId FROM POI WHERE Id = :id");
        $stmtVerify->execute([':id' => $id]);
        $verifyPoi = $stmtVerify->fetch(PDO::FETCH_ASSOC);
        
        if ($verifyPoi && $verifyPoi['ownerId'] == $user_id) {
            $stmtDelImg = $pdo->prepare("SELECT * FROM POIImage WHERE POIId = :poiId");
            $stmtDelImg->execute([':poiId' => $id]);
            while ($rowImg = $stmtDelImg->fetch(PDO::FETCH_ASSOC)) {
                $imgId = $rowImg['Id'] ?? $rowImg['id'] ?? null;
                syncPoiImageToFirebase($pdo, $imgId, 'delete');
            }
            $pdo->prepare("DELETE FROM POIImage WHERE POIId = :poiId")->execute([':poiId' => $id]);

            $stmt = $pdo->prepare("DELETE FROM POI WHERE Id = :id AND ownerId = :owner_id");
            $stmt->execute([':id' => $id, ':owner_id' => $user_id]);

            // Đồng bộ Firebase (Xoá)
            syncPoiToFirebase($pdo, $id, 'delete');
        }

        header("Location: index_partner.php");
        exit;
    } catch (Exception $e) {
        $error_msg = "Lỗi khi xoá: " . $e->getMessage();
    }
}

$searchQuery = isset($_GET['search']) ? trim($_GET['search']) : '';
$searchParam = '%' . $searchQuery . '%';
$filterStatus = isset($_GET['status']) ? $_GET['status'] : '';

$pois = [];
$error_msg = $error_msg ?? null;

try {
    $whereClauses = ["ownerId = :owner_id"];
    $params = [':owner_id' => $user_id];

    if ($searchQuery !== '') {
        $whereClauses[] = "Name LIKE :search";
        $params[':search'] = $searchParam;
    }
    
    if ($filterStatus !== '') {
        $whereClauses[] = "IsActive = :status";
        $params[':status'] = (int)$filterStatus;
    }

    $whereSql = " WHERE " . implode(' AND ', $whereClauses);
    
    // Sort by: Chờ duyệt (-1) -> Đang hoạt động (1) -> Tạm ngưng (0)
    $selectColumns = "p.*, 
            (SELECT imageUrl FROM POIImage WHERE poiId = p.Id AND imageType = 'avatar' LIMIT 1) as avatarUrl,
            (SELECT imageUrl FROM POIImage WHERE poiId = p.Id AND imageType = 'banner' LIMIT 1) as bannerUrl,
            (SELECT COUNT(id) FROM VisitLog WHERE poiId = p.Id) as visitCount,
            (SELECT COUNT(id) FROM AudioPlayLog WHERE poiId = p.Id) as audioPlayCount,
            (SELECT AVG(durationListened) FROM AudioPlayLog WHERE poiId = p.Id) as avgAudioDuration";

    $stmt = $pdo->prepare("SELECT $selectColumns FROM POI p" . $whereSql . " ORDER BY CASE p.IsActive WHEN -1 THEN 1 WHEN 1 THEN 2 WHEN 0 THEN 3 ELSE 4 END ASC, p.id DESC");
    
    foreach ($params as $key => $value) {
        $type = ($key === ':status' || $key === ':owner_id') ? PDO::PARAM_INT : PDO::PARAM_STR;
        $stmt->bindValue($key, $value, $type);
    }
    
    $stmt->execute();
    $pois = $stmt->fetchAll(PDO::FETCH_ASSOC);
} catch (Exception $e) {
    $error_msg = $e->getMessage();
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <link rel="icon" type="image/png" href="img/icon.png">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vendor Portal - Quản lý cửa hàng</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <!-- Track Asia API -->
    <script src="https://unpkg.com/trackasia-gl@1.0.5/dist/trackasia-gl.js"></script>
    <link href="https://unpkg.com/trackasia-gl@1.0.5/dist/trackasia-gl.css" rel="stylesheet" />
    <script src="https://cdn.jsdelivr.net/npm/@turf/turf@6/turf.min.js"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        brand: {
                            50: '#fff7ed',
                            100: '#ffedd5',
                            200: '#fed7aa',
                            300: '#fdba74',
                            400: '#fb923c',
                            500: '#f97316',
                            600: '#ea580c',
                        },
                        primary: '#ea580c'
                    }
                }
            }
        }
    </script>
</head>
<body class="bg-gray-50 text-gray-800 font-sans antialiased flex h-screen overflow-hidden">

    <!-- Sidebar -->
    <aside class="w-64 bg-white border-r border-gray-200 flex flex-col justify-between hidden md:flex">
        <div>
            <!-- Logo -->
            <div class="h-16 flex items-center px-6 border-b border-gray-100">
                <h1 class="text-xl font-bold text-primary flex items-center gap-2">
                    Vendor Portal
                </h1>
            </div>

            <!-- Navigation -->
            <nav class="p-4 space-y-1">
                <a href="dashboard_partner.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-th-large w-5 text-center"></i>
                    Tổng quan
                </a>
                <a href="index_partner.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
                    <i class="fas fa-store w-5 text-center"></i>
                    Cửa hàng của tôi
                </a>
                <a href="#" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-ticket-alt w-5 text-center"></i>
                    Khuyến mãi
                </a>
                <a href="#" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-cog w-5 text-center"></i>
                    Cài đặt
                </a>
            </nav>
        </div>

        <div class="p-4 border-t border-gray-200">
            <a href="logout.php" class="flex items-center gap-3 px-4 py-3 text-red-600 hover:bg-red-50 rounded-lg font-medium transition-colors">
                <i class="fas fa-sign-out-alt w-5 text-center"></i>
                Đăng xuất
            </a>
        </div>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 flex flex-col h-screen overflow-y-auto w-full relative">
        <!-- Top Header -->
        <header class="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 sticky top-0 z-10 w-full">
            <div class="flex items-center gap-4 flex-1">
                <button class="md:hidden text-gray-500 hover:text-gray-700">
                    <i class="fas fa-bars text-xl"></i>
                </button>
                <div class="relative w-full max-w-md hidden sm:block">
                    <i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
                    <input type="text" placeholder="Tìm cửa hàng, mã KM..." class="w-full pl-10 pr-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all text-sm">
                </div>
            </div>
            
            <div class="flex items-center gap-4">
                <button class="relative p-2 text-gray-400 hover:text-gray-600 transition-colors">
                    <i class="far fa-bell text-xl"></i>
                    <span class="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full border-2 border-white"></span>
                </button>
                <div class="h-8 w-px bg-gray-200 hidden sm:block"></div>
                <div class="flex items-center gap-3 cursor-pointer">
                    <div class="hidden sm:block text-right">
                        <div class="text-sm font-semibold text-gray-800"><?php echo htmlspecialchars($username); ?></div>
                        <div class="text-xs text-gray-500">Chủ cửa hàng / Đối tác</div>
                    </div>
                    <div class="w-10 h-10 rounded-full bg-brand-500 text-white flex items-center justify-center font-bold">
                        <i class="fas fa-user"></i>
                    </div>
                </div>
            </div>
        </header>

        <!-- Dashboard Content -->
        <div class="p-6 md:p-8 w-full max-w-7xl mx-auto space-y-6">
            
            <!-- Page Header -->
            <div class="bg-white rounded-xl border border-gray-100 p-6 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 shadow-sm">
                <div>
                    <h2 class="text-2xl font-bold text-gray-800">Quản Lý Cửa Hàng</h2>
                    <p class="text-sm text-gray-500 mt-1">Danh sách các địa điểm kinh doanh của bạn trên hệ thống bản đồ</p>
                </div>
                <button  onclick="openAddModal()" class="bg-brand-500 hover:bg-brand-600 text-white px-5 py-2.5 rounded-lg font-medium transition-colors flex items-center gap-2">
                    <i class="fas fa-plus"></i>
                    Đăng ký điểm mới
                </button>
            </div>

            <!-- Filters -->
            <div class="flex flex-col sm:flex-row justify-between gap-4">
                <form id="searchForm" action="index_partner.php" method="GET" class="w-full flex flex-col sm:flex-row justify-between gap-4">
                    <div class="relative w-full sm:w-[500px]">
                        <i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
                        <input type="text" id="searchInput" name="search" placeholder="Tìm kiếm cửa hàng của bạn..." value="<?php echo htmlspecialchars($searchQuery); ?>" class="w-full pl-10 pr-4 py-2.5 bg-white border border-gray-200 rounded-lg focus:outline-none focus:border-brand-500 transition-all text-sm shadow-sm" oninput="debounceSearch()">
                    </div>
                    <div class="w-full sm:w-48 relative">
                        <select name="status" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-lg focus:outline-none focus:border-brand-500 text-sm text-gray-700 appearance-none shadow-sm bg-[url('data:image/svg+xml;charset=US-ASCII,%3Csvg%20width%3D%2220%22%20height%3D%2220%22%20xmlns%3D%22http%3A%2F%2Fwww.w3.org%2F2000%2Fsvg%22%3E%3Cpath%20d%3D%22M5%208l5%205%205-5%22%20stroke%3D%22%236b7280%22%20stroke-width%3D%222%22%20fill%3D%22none%22%20stroke-linecap%3D%22round%22%20stroke-linejoin%3D%22round%22%2F%3E%3C%2Fsvg%3E')] bg-no-repeat bg-[position:right_10px_center]" onchange="document.getElementById('searchForm').submit()">
                            <option value="">Tất cả trạng thái</option>
                            <option value="1" <?php echo ($filterStatus === '1') ? 'selected' : ''; ?>>Đang hoạt động</option>
                            <option value="0" <?php echo ($filterStatus === '0') ? 'selected' : ''; ?>>Tạm ngưng</option>
                            <option value="-1" <?php echo ($filterStatus === '-1') ? 'selected' : ''; ?>>Chờ duyệt</option>
                        </select>
                    </div>
                </form>
                <script>
                    let searchTimeout;
                    function debounceSearch() {
                        clearTimeout(searchTimeout);
                        // Tự động submit form sau 500ms ngừng gõ
                        searchTimeout = setTimeout(function() {
                            document.getElementById('searchForm').submit();
                        }, 500);
                    }
                    
                    // Tự động focus lại vào ô tìm kiếm và đặt con trỏ ở cuối sau khi tải lại trang
                    document.addEventListener("DOMContentLoaded", function() {
                        const input = document.getElementById('searchInput');
                        if (input && input.value) {
                            input.focus();
                            const val = input.value;
                            input.value = '';
                            input.value = val;
                        }
                    });
                </script>
            </div>

            <!-- Store Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                
                <?php if (!empty($error_msg)): ?>
                    <div class="col-span-full p-4 bg-red-50 text-red-600 border border-red-200 rounded-lg">
                        <i class="fas fa-exclamation-triangle"></i> Lỗi truy vấn: <?php echo htmlspecialchars($error_msg); ?> (ID hiện tại: <?php echo $user_id; ?>)
                    </div>
                <?php endif; ?>

                <?php if (empty($pois)): ?>
                    <!-- Hiện thông báo khi chưa có cửa hàng nào -->
                    <div class="col-span-full pt-8 pb-12 flex flex-col items-center justify-center text-gray-500">
                        <i class="fas fa-store-slash text-4xl mb-4 text-gray-300"></i>
                        <p>Bạn chưa có địa điểm nào trên hệ thống. (User ID hiện tại: <?php echo $user_id; ?>)</p>
                    </div>
                <?php else: ?>
                    <?php foreach ($pois as $poi): 
                        // imageUrls là JSON hoặc link ảnh, tạm hiện ảnh đầu tiên hoặc fallback
                        $imageUrl = '';
                        if (!empty($poi['imageUrls'])) {
                            $images = json_decode($poi['imageUrls'], true);
                            $imageUrl = is_array($images) && count($images) > 0 ? $images[0] : (is_string($poi['imageUrls']) && filter_var($poi['imageUrls'], FILTER_VALIDATE_URL) ? $poi['imageUrls'] : '');
                        }
                    ?>
                        <!-- Thẻ POI -->
                        <div class="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden flex flex-col group <?php echo ($poi['isActive'] != 1) ? 'opacity-90' : ''; ?>">
                            <div class="h-48 bg-gray-200 relative mb-0 cursor-pointer" onclick='javascript:openPoiModal(<?php echo htmlspecialchars(json_encode($poi, JSON_UNESCAPED_UNICODE), ENT_QUOTES, "UTF-8"); ?>)'>
                                <?php if (!empty($imageUrl)): ?>
                                    <img src="<?php echo htmlspecialchars($imageUrl); ?>" alt="<?php echo htmlspecialchars($poi['name']); ?>" class="w-full h-full object-cover cursor-pointer <?php echo ($poi['isActive'] != 1) ? 'opacity-60 mix-blend-overlay' : ''; ?>" onclick='javascript:openPoiModal(<?php echo htmlspecialchars(json_encode($poi, JSON_UNESCAPED_UNICODE), ENT_QUOTES, "UTF-8"); ?>)'>
                                <?php else: ?>
                                    <div class="w-full h-full flex items-center justify-center bg-gray-100 text-gray-400">
                                        <i class="fas fa-image text-3xl"></i>
                                    </div>
                                <?php endif; ?>
                                
                                <?php if ($poi['isActive'] == 1): ?>
                                    <span class="absolute top-3 left-3 bg-green-500 text-white text-[11px] font-bold px-3 py-1 rounded-full uppercase tracking-wider shadow-sm">Đang hoạt động</span>
                                <?php elseif ($poi['isActive'] == 0): ?>
                                    <span class="absolute top-3 left-3 bg-gray-500 text-white text-[11px] font-bold px-3 py-1 rounded-full uppercase tracking-wider shadow-sm">Tạm ngưng</span>
                                <?php else: ?>
                                    <span class="absolute top-3 left-3 bg-amber-500 text-white text-[11px] font-bold px-3 py-1 rounded-full uppercase tracking-wider shadow-sm">Chờ duyệt</span>
                                <?php endif; ?>
                            </div>
                            <div class="p-5 flex-1 flex flex-col pt-4">
                                <div class="flex justify-between items-start mb-2">
                                    <h3 class="text-lg font-bold text-gray-800 leading-tight"><?php echo htmlspecialchars($poi['name'] ?? 'Chưa có tên'); ?></h3>
                                </div>
                                <p class="text-gray-500 text-sm mb-4 flex items-start gap-2 line-clamp-2">
                                    <i class="fas fa-map-marker-alt mt-0.5 text-gray-400"></i>
                                    <?php echo htmlspecialchars($poi['address'] ?? 'Chưa có thông tin địa điểm'); ?>
                                </p>
                                
                                <div class="grid grid-cols-2 gap-4 mb-5 pt-4 border-t border-gray-100 mt-auto">
                                    <div>
                                        <div class="text-xs text-gray-400 mb-1 font-medium select-none">Bán kính kích hoạt</div>
                                        <div class="font-bold text-gray-700 text-base"><?php echo htmlspecialchars($poi['triggerRadiusMeters'] ?? 0); ?>m</div>
                                    </div>
                                    <div class="text-right">
                                        <div class="text-xs text-gray-400 mb-1 font-medium select-none">ID Hệ thống</div>
                                        <div class="font-bold text-brand-600 text-base flex items-center justify-end gap-1">
                                            #<?php echo htmlspecialchars($poi['id']); ?>
                                        </div>
                                    </div>
                                </div>

                                <div class="grid grid-cols-2 gap-3 mt-1">
                                    <button onclick='openEditModal(<?php echo htmlspecialchars(json_encode($poi), ENT_QUOTES, "UTF-8"); ?>)' class="px-3 py-2 border border-gray-200 text-gray-700 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors flex items-center justify-center gap-2">
                                        <i class="fas fa-pen"></i> Chỉnh sửa
                                    </button>
                                    <button class="px-3 py-2 border border-brand-200 bg-brand-50 text-brand-600 rounded-lg text-sm font-medium hover:bg-brand-100 transition-colors flex items-center justify-center gap-2">
                                        <i class="fas fa-music"></i> Audio & Menu
                                    </button>
                                </div>
                            </div>
                        </div>
                    <?php endforeach; ?>
                <?php endif; ?>

                <!-- Add New Card -->
                <button onclick="openAddModal()" class="bg-transparent border-2 border-dashed border-gray-300 rounded-xl flex flex-col items-center justify-center p-8 hover:bg-brand-50/50 hover:border-brand-400 hover:text-brand-600 transition-all min-h-[400px] group cursor-pointer text-gray-500 <?php echo empty($pois) ? 'col-span-full md:col-span-1' : ''; ?>">
                    <div class="w-16 h-16 rounded-full bg-white border border-gray-200 flex items-center justify-center text-2xl mb-4 group-hover:border-brand-300 group-hover:bg-brand-100 group-hover:text-brand-600 shadow-sm transition-all text-gray-400">
                        <i class="fas fa-plus"></i>
                    </div>
                    <h3 class="font-bold text-gray-800 text-lg group-hover:text-brand-600 transition-colors">Thêm Điểm Kinh Doanh</h3>
                    <p class="text-sm text-center mt-2 max-w-[220px]">Đăng ký thêm cơ sở mới để hiển thị trên bản đồ Vĩnh Khánh</p>
                </button>

            </div>

        </div>
    </main>

    <!-- Modal Add POI -->
    <div id="addModal" class="hidden fixed inset-0 z-50 overflow-y-auto bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col relative overflow-hidden">
            
            <div class="px-6 py-4 border-b border-gray-100 flex justify-between items-center bg-white/80 backdrop-blur-md sticky top-0 z-10">
                <h3 class="text-xl font-bold text-gray-800 flex items-center gap-2">
                    <i class="fas fa-plus-circle text-primary"></i> Đăng ký địa điểm mới
                </h3>
                <button type="button" onclick="closeAddModal()" class="w-8 h-8 flex items-center justify-center rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors">
                    <i class="fas fa-times text-lg"></i>
                </button>
            </div>

            <form action="index_partner.php" method="POST" enctype="multipart/form-data" class="overflow-y-auto p-6 scroll-smooth" id="addPoiForm" onsubmit="if(this.submitted) return false; this.submitted=true;">
                <input type="hidden" name="action" value="add_poi">
                <input type="hidden" name="has_en" id="add_has_en" value="1">
                <input type="hidden" name="has_zh" id="add_has_zh" value="1">
                <input type="hidden" name="has_ja" id="add_has_ja" value="1">
                <input type="hidden" name="has_ko" id="add_has_ko" value="1">
                <input type="hidden" name="has_fr" id="add_has_fr" value="1">
                <input type="hidden" name="has_ru" id="add_has_ru" value="1">

                <div class="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6">
                    <!-- Column 1 -->
                    <div class="space-y-5">
                        <div class="group">
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5 group-focus-within:text-primary transition-colors">Tên địa điểm (Name) <span class="text-red-500">*</span></label>
                            <input type="text" name="name" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>

                        <div class="grid grid-cols-2 gap-4 group">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5 focus-within:text-primary transition-colors">Vĩ độ (Latitude) <span class="text-red-500">*</span></label>
                                <input type="number" step="any" name="latitude" id="add_latitude" value="0" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5 focus-within:text-primary transition-colors">Kinh độ (Longitude) <span class="text-red-500">*</span></label>
                                <input type="number" step="any" name="longitude" id="add_longitude" value="0" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                        </div>

                        <!-- Add Modal Map Container -->
                        <div class="mt-4">
                            <div class="flex justify-between items-center mb-1.5">
                                <label class="text-sm font-semibold text-gray-700">Chọn vị trí trên bản đồ</label>
                                <span class="text-xs text-gray-500 italic"><i class="fas fa-mouse-pointer mr-1"></i>Click trên bản đồ để tự động lấy tọa độ</span>
                            </div>
                            <div class="h-44 w-full rounded-xl overflow-hidden border border-gray-200 bg-gray-100 relative">
                                <div id="add_map_canvas" class="absolute inset-0 w-full h-full"></div>
                            </div>
                        </div>

                        <div class="grid grid-cols-2 gap-4">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">SĐT (Phone) <span class="text-red-500">*</span></label>
                                <input type="text" name="phone" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Bán kính (m) <span class="text-red-500">*</span></label>
                                <input type="number" name="triggerRadiusMeters" value="20" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Địa chỉ (Address) <span class="text-red-500">*</span></label>
                            <input type="text" name="address" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Trạng thái (IsActive)</label>
                            <select name="isActive" class="w-full px-4 py-2.5 bg-gray-100 border border-gray-200 rounded-xl text-gray-500 transition-all text-sm outline-none appearance-none pointer-events-none cursor-not-allowed">
                                <option value="-1" selected>⚪ Chờ duyệt (Mặc định)</option>
                            </select>
                        </div>

                        <div class="grid grid-cols-2 gap-4">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Ảnh Avatar</label>
                                <input type="file" name="avatar" accept="image/*" class="w-full px-3 py-2 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Ảnh Banner</label>
                                <input type="file" name="banner" accept="image/*" class="w-full px-3 py-2 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm">
                            </div>
                        </div>
                        
                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Hình ảnh (ImageUrls JSON)</label>
                            <textarea name="imageUrls" rows="2" placeholder='["link_avatar", "link_banner"]' class="w-full px-4 py-3 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono resize-none"></textarea>
                            <p class="text-xs text-gray-500 mt-1">Hoặc upload ảnh ở trên. Nếu có upload, ảnh sẽ đè lên mảng JSON này.</p>
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">MapLink</label>
                            <input type="url" name="mapLink" class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>
                    </div>

                    <!-- Column 2 (Languages) -->
                    <div class="space-y-5 bg-gray-50/30 p-5 rounded-2xl border border-gray-100">
                        <div class="flex items-center gap-2 mb-2 pb-2 border-b border-gray-200">
                            <i class="fas fa-language text-brand-500"></i>
                            <h4 class="font-semibold text-gray-800">Nội dung & Thuyết minh</h4>
                        </div>

                        <!-- Tiếng Việt -->
                        <div class="space-y-3">
                            <div class="flex items-center justify-between">
                                <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-red-600 rounded-sm inline-block relative overflow-hidden"><span class="absolute inset-0 flex items-center justify-center text-[8px] text-yellow-300">★</span></span> Tiếng Việt (Mặc định)</h5>
                                <button type="button" onclick="autoTranslateAdd()" class="text-[11px] font-medium bg-blue-50 text-blue-600 px-2 py-1 rounded-md hover:bg-blue-100 transition-colors flex items-center gap-1 border border-blue-200" title="Dịch tự động nội dung Tiếng Việt sang English và Chinese">
                                    <i class="fas fa-language"></i> <span id="translateBtnTextAdd">Dịch tự động</span>
                                </button>
                            </div>
                            <div>
                                <textarea id="add_descriptionText" name="descriptionText" rows="2" required placeholder="Mô tả tóm tắt... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScript" name="ttsScript" rows="2" required placeholder="Nội dung đọc TTS... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- English -->
                        <div id="add_en_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-blue-800 rounded-sm inline-block relative overflow-hidden"><span class="absolute top-0 left-0 w-2 h-2 bg-white"></span><span class="absolute top-0 right-0 w-2 h-2 bg-red-600"></span><span class="absolute bottom-0 left-0 w-2 h-2 bg-red-600"></span><span class="absolute bottom-0 right-0 w-2 h-2 bg-white"></span></span> English</h5>
                            <div>
                                <textarea id="add_descriptionEn" name="descriptionEn" rows="2" required placeholder="English description... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptEn" name="ttsScriptEn" rows="2" required placeholder="TTS Script in English... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Chinese -->
                        <div id="add_zh_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-red-600 rounded-sm inline-block relative overflow-hidden"><span class="absolute top-0.5 left-0.5 text-[6px] text-yellow-300">★</span></span> 中文 (Chinese)</h5>
                            <div>
                                <textarea id="add_descriptionZh" name="descriptionZh" rows="2" required placeholder="中文简介... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptZh" name="ttsScriptZh" rows="2" required placeholder="TTS 语音内容... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Japanese -->
                        <div id="add_ja_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-0 m-auto w-2 h-2 bg-red-600 rounded-full"></span></span> 日本语 (Japanese)</h5>
                            <div>
                                <textarea id="add_descriptionJa" name="descriptionJa" rows="2" required placeholder="日本語の説明... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptJa" name="ttsScriptJa" rows="2" required placeholder="TTS音声内容... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Korean -->
                        <div id="add_ko_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-0 m-auto w-1.5 h-1.5 bg-red-600 rounded-full"></span><span class="absolute inset-0 m-auto w-1.5 h-1.5 bg-blue-600 rounded-full translate-y-1"></span></span> 한국어 (Korean)</h5>
                            <div>
                                <textarea id="add_descriptionKo" name="descriptionKo" rows="2" required placeholder="한국어 설명... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptKo" name="ttsScriptKo" rows="2" required placeholder="TTS 음성 내용... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- French -->
                        <div id="add_fr_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-y-0 left-0 w-1.5 bg-blue-600"></span><span class="absolute inset-y-0 right-0 w-1.5 bg-red-600"></span></span> Français (French)</h5>
                            <div>
                                <textarea id="add_descriptionFr" name="descriptionFr" rows="2" required placeholder="Description en français... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptFr" name="ttsScriptFr" rows="2" required placeholder="Contenu audio TTS... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Russian -->
                        <div id="add_ru_container" class="space-y-3 pt-3 border-t border-gray-100 relative">
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute top-0 inset-x-0 h-1 bg-white"></span><span class="absolute inset-x-0 top-1 h-1 bg-blue-600"></span><span class="absolute bottom-0 inset-x-0 h-1 bg-red-600"></span></span> Русский (Russian)</h5>
                            <div>
                                <textarea id="add_descriptionRu" name="descriptionRu" rows="2" required placeholder="Описание на русском... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="add_ttsScriptRu" name="ttsScriptRu" rows="2" required placeholder="Текст для аудио... *" class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                    </div>
                </div>

                <div class="pt-6 mt-6 border-t border-gray-100 flex justify-end gap-3 sticky bottom-0 bg-white z-10 pb-2">
                    <button type="button" onclick="closeAddModal()" class="px-6 py-2.5 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-gray-50 hover:text-gray-800 transition-colors shadow-sm">
                        <i class="fas fa-times mr-1.5 text-gray-400"></i> Hủy
                    </button>
                    <button type="submit" class="px-6 py-2.5 text-sm font-medium text-white bg-primary rounded-xl hover:bg-brand-600 focus:ring-4 focus:ring-brand-500/30 transition-all shadow-sm shadow-brand-500/30">
                        <i class="fas fa-plus mr-1.5"></i> Tạo POI
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Modal Detail POI -->
﻿                <div id="poiDetailModal" class="hidden fixed inset-0 z-50 overflow-y-auto bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 transition-all duration-300 opacity-0 translate-y-4">
                    <div class="bg-white rounded-2xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col relative overflow-y-auto"><div class="sticky top-0 w-full flex justify-between items-center px-6 py-4 bg-white/90 backdrop-blur z-30 border-b border-gray-100 shadow-sm">
                        <h3 class="text-lg font-bold text-gray-800" id="modalTitle">Chi tiết quán ăn</h3>
                        <button onclick="closePoiModal()" class="w-8 h-8 rounded-full bg-gray-100 hover:bg-red-100 hover:text-red-500 flex items-center justify-center transition-colors">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>

                    <div class="p-6 w-full max-w-4xl mx-auto space-y-6">
                        <!-- Header Images -->
                        <div class="relative w-full h-48 md:h-64 rounded-2xl overflow-hidden bg-gray-200 border border-gray-100 shadow-inner">
                            <img id="modalBanner" src="" class="w-full h-full object-cover object-center" alt="Banner" onerror="this.src='https://via.placeholder.com/1200x400?text=No+Banner'">
                            
                            <div class="absolute -bottom-6 left-6 w-24 h-24 md:w-32 md:h-32 rounded-xl border-4 border-white bg-white shadow-lg overflow-hidden z-10">
                                <img id="modalAvatar" src="" class="w-full h-full object-cover" alt="Avatar" onerror="this.src='https://via.placeholder.com/300x300?text=No+Avatar'">
                            </div>

                            <div class="absolute top-4 right-4" id="modalStatusBadge">
                                <!-- Status goes here -->
                            </div>
                        </div>

                        <!-- Basic Information -->
                        <div class="pt-8 md:pt-10 grid grid-cols-1 md:grid-cols-4 gap-6">
                            <div class="md:col-span-2 space-y-4">
                                <div>
                                    <h2 id="modalName" class="text-2xl md:text-3xl font-extrabold text-gray-900 leading-tight">Name</h2>
                                    <p id="modalAddress" class="text-gray-500 mt-2 flex items-start gap-2">
                                        <i class="fas fa-map-marker-alt text-brand-500 mt-1"></i> <span>Address</span>
                                    </p>
                                </div>
                                
                                <div class="flex flex-wrap gap-4 text-sm">
                                    <div class="flex items-center gap-2 bg-gray-50 px-3 py-2 rounded-lg border border-gray-100 shadow-sm">
                                        <i class="fas fa-phone-alt text-blue-500"></i> <span id="modalPhone" class="font-medium text-gray-700">Phone</span>
                                    </div>
                                    <div class="flex items-center gap-2 bg-gray-50 px-3 py-2 rounded-lg border border-gray-100 shadow-sm">
                                        <i class="fas fa-bullseye text-primary"></i> <span class="font-medium text-gray-700">Bán kính: <span id="modalRadius">0</span>m</span>
                                    </div>
                                    <a id="modalMapLink" href="#" target="_blank" class="flex items-center gap-2 bg-blue-50 text-blue-600 hover:bg-blue-100 px-3 py-2 rounded-lg border border-blue-200 shadow-sm transition-colors">
                                        <i class="fas fa-directions"></i> <span class="font-medium">Mở Google Maps</span>
                                    </a>
                                </div>
                            </div>

                            <!-- Statistics -->
                            <div class="bg-gradient-to-br from-brand-50 to-orange-50 p-5 rounded-2xl border border-brand-100 shadow-sm flex flex-col justify-center space-y-3">
                                <h4 class="font-bold text-brand-800 text-sm border-b border-brand-200/50 pb-2 mb-1"><i class="fas fa-chart-bar mr-2"></i>Thống kê tương tác</h4>
                                <div class="flex justify-between items-center text-sm">
                                    <span class="text-gray-600">Lượt vào vùng (Visit):</span>
                                    <span id="modalVisits" class="font-bold text-gray-900 bg-white px-2 py-0.5 rounded shadow-sm">0</span>
                                </div>
                                <div class="flex justify-between items-center text-sm">
                                    <span class="text-gray-600">Lượt nghe Audio:</span>
                                    <span id="modalAudioPlays" class="font-bold text-gray-900 bg-white px-2 py-0.5 rounded shadow-sm">0</span>
                                </div>
                                <div class="flex justify-between items-center text-sm">
                                    <span class="text-gray-600">Nghe trung bình:</span>
                                    <span class="font-bold text-gray-900 bg-white px-2 py-0.5 rounded shadow-sm"><span id="modalAvgDuration">0</span>s</span>
                                </div>
                            </div>

                            <!-- QR Code -->
                            <div class="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex flex-col items-center justify-center space-y-2">
                                <h4 class="font-bold text-gray-800 text-sm mb-2 text-center text-brand-600"><i class="fas fa-qrcode mr-1"></i>Mã QR</h4>
                                <img id="modalQrCode" src="" alt="QR Code" class="w-32 h-32 object-contain shadow-sm border border-gray-200 rounded-lg p-2 bg-white">
                                <p class="text-[11px] text-gray-500 text-center font-medium">Quét để <br>nghe thuyết minh</p>
                            </div>
                        </div>

                        <!-- Content sections for languages -->
                        <div class="mt-8 border border-gray-200 rounded-2xl bg-white shadow-sm overflow-hidden">
                            <div class="flex border-b border-gray-200 overflow-x-auto no-scrollbar bg-gray-50" id="langTabs">
                                <!-- Tabs generated via JS -->
                            </div>
                            <div class="p-5" id="langContent">
                                <!-- Content generated via JS -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>

    <!-- Modal Edit POI -->
    <div id="editModal" class="hidden fixed inset-0 z-50 overflow-y-auto bg-black/50 backdrop-blur-sm flex items-center justify-center p-4">
        <div class="bg-white rounded-2xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col relative overflow-hidden">
            
            <div class="px-6 py-4 border-b border-gray-100 flex justify-between items-center bg-white/80 backdrop-blur-md sticky top-0 z-10">
                <h3 class="text-xl font-bold text-gray-800 flex items-center gap-2">
                    <i class="fas fa-edit text-primary"></i> Chỉnh sửa địa điểm
                </h3>
                <button type="button" onclick="closeEditModal()" class="w-8 h-8 flex items-center justify-center rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors">
                    <i class="fas fa-times text-lg"></i>
                </button>
            </div>

<form id="editPoiForm" action="index_partner.php" method="POST" enctype="multipart/form-data" class="overflow-y-auto p-6 scroll-smooth" onsubmit="if(this.submitted) return false; this.submitted=true;">
                  <input type="hidden" name="action" value="update_poi">
                  <input type="hidden" name="id" id="edit_id">
                  <input type="hidden" name="has_en" id="edit_has_en" value="0">
                  <input type="hidden" name="has_zh" id="edit_has_zh" value="0">
                  <input type="hidden" name="has_ja" id="edit_has_ja" value="0">
                  <input type="hidden" name="has_ko" id="edit_has_ko" value="0">
                  <input type="hidden" name="has_fr" id="edit_has_fr" value="0">
                  <input type="hidden" name="has_ru" id="edit_has_ru" value="0">

                <div class="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6">
                    <!-- Column 1 -->
                    <div class="space-y-5">
                        <div class="group">
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5 group-focus-within:text-primary transition-colors">Tên địa điểm (Name) <span class="text-red-500">*</span></label>
                            <input type="text" name="name" id="edit_name" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>

                        <div class="grid grid-cols-2 gap-4 group">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5 focus-within:text-primary transition-colors">Vĩ độ (Latitude) <span class="text-red-500">*</span></label>
                                <input type="number" step="any" name="latitude" id="edit_latitude" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5 focus-within:text-primary transition-colors">Kinh độ (Longitude) <span class="text-red-500">*</span></label>
                                <input type="number" step="any" name="longitude" id="edit_longitude" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                        </div>

                        <!-- Edit Modal Map Container -->
                        <div class="mt-4">
                            <div class="flex justify-between items-center mb-1.5">
                                <label class="text-sm font-semibold text-gray-700">Chọn vị trí trên bản đồ</label>
                                <span class="text-xs text-gray-500 italic"><i class="fas fa-mouse-pointer mr-1"></i>Click trên bản đồ để tự động lấy tọa độ</span>
                            </div>
                            <div class="h-44 w-full rounded-xl overflow-hidden border border-gray-200 bg-gray-100 relative">
                                <div id="edit_map_canvas" class="absolute inset-0 w-full h-full"></div>
                            </div>
                        </div>

                        <div class="grid grid-cols-2 gap-4">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">SĐT (Phone)</label>
                                <input type="text" name="phone" id="edit_phone" class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Bán kính quét (m)</label>
                                <input type="number" name="triggerRadiusMeters" id="edit_triggerRadiusMeters" required class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono">
                            </div>
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Địa chỉ (Address)</label>
                            <input type="text" name="address" id="edit_address" class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">Trạng thái (IsActive)</label>
                            <select name="isActive" id="edit_isActive" class="w-full px-4 py-2.5 bg-gray-100 border border-gray-200 rounded-xl text-gray-600 transition-all text-sm outline-none appearance-none pointer-events-none cursor-not-allowed">
                                <option value="1">🟢 Hoạt động</option>
                                <option value="0">🔴 Tạm ngưng</option>
                                <option value="-1">⚪ Chờ Duyệt</option>
                            </select>
                            <p class="text-[11px] text-amber-600 mt-1.5 italic"><i class="fas fa-info-circle mr-1"></i>Lưu ý: Sau khi lưu thay đổi, địa điểm sẽ tạm tự động chuyển về trạng thái chờ Admin duyệt lại.</p>
                        </div>

                        <div class="grid grid-cols-2 gap-4">
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Ảnh Avatar</label>
                                <input type="file" name="avatar" accept="image/*" class="w-full px-3 py-2 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm">
                            </div>
                            <div>
                                <label class="block text-sm font-semibold text-gray-700 mb-1.5">Ảnh Banner</label>
                                <input type="file" name="banner" accept="image/*" class="w-full px-3 py-2 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm">
                            </div>
                        </div>
                        
                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5 flex items-center justify-between">
                                Hình ảnh (ImageUrls JSON) 
                                <span class="text-xs font-normal text-gray-400 bg-gray-100 px-2 py-0.5 rounded">["avatar", "banner"]</span>
                            </label>
                            <textarea name="imageUrls" id="edit_imageUrls" rows="2" class="w-full px-4 py-3 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none font-mono resize-none"></textarea>
                            <p class="text-xs text-gray-500 mt-1">Hoặc upload ảnh ở trên. Nếu có upload, ảnh sẽ được thay thế đúng vị trí tương ứng trong mảng JSON liên kết.</p>
                        </div>

                        <div>
                            <label class="block text-sm font-semibold text-gray-700 mb-1.5">MapLink (Google Maps / URL)</label>
                            <input type="url" name="mapLink" id="edit_mapLink" class="w-full px-4 py-2.5 bg-gray-50/50 border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none">
                        </div>
                    </div>

                    <!-- Column 2 (Languages) -->
                    <div class="space-y-5 bg-gray-50/30 p-5 rounded-2xl border border-gray-100">
                        <div class="flex items-center gap-2 mb-2 pb-2 border-b border-gray-200">
                            <i class="fas fa-language text-brand-500"></i>
                            <h4 class="font-semibold text-gray-800">Nội dung & Thuyết minh</h4>
                        </div>

                        <!-- Tiếng Việt -->
                        <div class="space-y-3">
                            <div class="flex items-center justify-between">
                                <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-red-600 rounded-sm inline-block relative overflow-hidden"><span class="absolute inset-0 flex items-center justify-center text-[8px] text-yellow-300">★</span></span> Tiếng Việt (Mặc định)</h5>
                                <button type="button" onclick="autoTranslate()" class="text-[11px] font-medium bg-blue-50 text-blue-600 px-2 py-1 rounded-md hover:bg-blue-100 transition-colors flex items-center gap-1 border border-blue-200" title="Dịch tự động nội dung Tiếng Việt sang English và Chinese">
                                    <i class="fas fa-language"></i> <span id="translateBtnText">Dịch tự động</span>
                                </button>
                            </div>
                            <div>
                                <textarea name="descriptionText" id="edit_descriptionText" rows="2" placeholder="Mô tả tóm tắt..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea name="ttsScript" id="edit_ttsScript" rows="2" placeholder="Nội dung đọc TTS..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

<!-- Buttons Add Lang (Edit Modal) -->
                          <div class="flex flex-wrap gap-2">
                              <button type="button" onclick="showEditLang('en')" id="btn_edit_en" class="px-3 py-1.5 text-xs font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-lg hover:bg-blue-100 transition-colors">
                                  + Thêm tiếng Anh
                              </button>
                              <button type="button" onclick="showEditLang('zh')" id="btn_edit_zh" class="px-3 py-1.5 text-xs font-medium text-red-600 bg-red-50 border border-red-200 rounded-lg hover:bg-red-100 transition-colors">
                                  + Thêm tiếng Trung
                              </button>
                              <button type="button" onclick="showEditLang('ja')" id="btn_edit_ja" class="px-3 py-1.5 text-xs font-medium text-purple-600 bg-purple-50 border border-purple-200 rounded-lg hover:bg-purple-100 transition-colors">
                                  + Thêm tiếng Nhật
                              </button>
                              <button type="button" onclick="showEditLang('ko')" id="btn_edit_ko" class="px-3 py-1.5 text-xs font-medium text-indigo-600 bg-indigo-50 border border-indigo-200 rounded-lg hover:bg-indigo-100 transition-colors">
                                  + Thêm tiếng Hàn
                              </button>
                              <button type="button" onclick="showEditLang('fr')" id="btn_edit_fr" class="px-3 py-1.5 text-xs font-medium text-blue-800 bg-blue-100 border border-blue-300 rounded-lg hover:bg-blue-200 transition-colors">
                                  + Thêm tiếng Pháp
                              </button>
                              <button type="button" onclick="showEditLang('ru')" id="btn_edit_ru" class="px-3 py-1.5 text-xs font-medium text-red-800 bg-red-100 border border-red-300 rounded-lg hover:bg-red-200 transition-colors">
                                  + Thêm tiếng Nga
                              </button>
                          </div>

                          <!-- English -->
                          <div id="edit_en_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                              <button type="button" onclick="hideEditLang('en')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                              <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-blue-800 rounded-sm inline-block relative overflow-hidden"><span class="absolute top-0 left-0 w-2 h-2 bg-white"></span><span class="absolute top-0 right-0 w-2 h-2 bg-red-600"></span><span class="absolute bottom-0 left-0 w-2 h-2 bg-red-600"></span><span class="absolute bottom-0 right-0 w-2 h-2 bg-white"></span></span> English</h5>
                              <div>
                                  <textarea name="descriptionEn" id="edit_descriptionEn" rows="2" placeholder="English description..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                              </div>
                              <div>
                                  <textarea name="ttsScriptEn" id="edit_ttsScriptEn" rows="2" placeholder="TTS Script in English..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                              </div>
                          </div>

                          <!-- Chinese -->
                          <div id="edit_zh_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                              <button type="button" onclick="hideEditLang('zh')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-red-600 rounded-sm inline-block relative overflow-hidden"><span class="absolute top-0.5 left-0.5 text-[6px] text-yellow-300">★</span></span> 中文 (Chinese)</h5>
                            <div>
                                <textarea name="descriptionZh" id="edit_descriptionZh" rows="2" placeholder="中文简介..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea name="ttsScriptZh" id="edit_ttsScriptZh" rows="2" placeholder="TTS 语音内容..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Japanese -->
                        <div id="edit_ja_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                            <button type="button" onclick="hideEditLang('ja')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-0 m-auto w-2 h-2 bg-red-600 rounded-full"></span></span> 日本语 (Japanese)</h5>
                            <div>
                                <textarea id="edit_descriptionJa" name="descriptionJa" rows="2" placeholder="日本語の説明..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="edit_ttsScriptJa" name="ttsScriptJa" rows="2" placeholder="TTS音声内容..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Korean -->
                        <div id="edit_ko_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                            <button type="button" onclick="hideEditLang('ko')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-0 m-auto w-1.5 h-1.5 bg-red-600 rounded-full"></span><span class="absolute inset-0 m-auto w-1.5 h-1.5 bg-blue-600 rounded-full translate-y-1"></span></span> 한국어 (Korean)</h5>
                            <div>
                                <textarea id="edit_descriptionKo" name="descriptionKo" rows="2" placeholder="한국어 설명..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="edit_ttsScriptKo" name="ttsScriptKo" rows="2" placeholder="TTS 음성 내용..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- French -->
                        <div id="edit_fr_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                            <button type="button" onclick="hideEditLang('fr')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute inset-y-0 left-0 w-1.5 bg-blue-600"></span><span class="absolute inset-y-0 right-0 w-1.5 bg-red-600"></span></span> Français (French)</h5>
                            <div>
                                <textarea id="edit_descriptionFr" name="descriptionFr" rows="2" placeholder="Description en français..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="edit_ttsScriptFr" name="ttsScriptFr" rows="2" placeholder="Contenu audio TTS..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                        <!-- Russian -->
                        <div id="edit_ru_container" class="hidden space-y-3 pt-3 border-t border-gray-100 relative">
                            <button type="button" onclick="hideEditLang('ru')" class="absolute top-3 right-0 text-gray-400 hover:text-red-500 transition-colors"><i class="fas fa-times"></i></button>
                            <h5 class="text-xs font-bold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><span class="w-4 h-3 bg-white rounded-sm inline-block relative overflow-hidden border border-gray-200"><span class="absolute top-0 inset-x-0 h-1 bg-white"></span><span class="absolute inset-x-0 top-1 h-1 bg-blue-600"></span><span class="absolute bottom-0 inset-x-0 h-1 bg-red-600"></span></span> Русский (Russian)</h5>
                            <div>
                                <textarea id="edit_descriptionRu" name="descriptionRu" rows="2" placeholder="Описание на русском..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                            <div>
                                <textarea id="edit_ttsScriptRu" name="ttsScriptRu" rows="2" placeholder="Текст для аудио..." class="w-full px-4 py-2.5 bg-white border border-gray-200 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-sm outline-none resize-none"></textarea>
                            </div>
                        </div>

                    </div>
                </div>

                <div class="pt-6 mt-6 border-t border-gray-100 flex justify-end gap-3 sticky bottom-0 bg-white z-10 pb-2">
                    <button type="button" onclick="closeEditModal()" class="px-6 py-2.5 text-sm font-medium text-gray-600 bg-white border border-gray-200 rounded-xl hover:bg-gray-50 hover:text-gray-800 transition-colors shadow-sm">
                        <i class="fas fa-times mr-1.5 text-gray-400"></i> Hủy
                    </button>
                    <button type="submit" class="px-6 py-2.5 text-sm font-medium text-white bg-primary rounded-xl hover:bg-brand-600 focus:ring-4 focus:ring-brand-500/30 transition-all shadow-sm shadow-brand-500/30">
                        <i class="fas fa-check mr-1.5"></i> Lưu thay đổi
                    </button>
                </div>
            </form>
        </div>
    </div>

    <?php if (isset($errorMsg)): ?>
    <script>
        alert('<?php echo addslashes($errorMsg); ?>');
    </script>
    <?php endif; ?>

    <!-- Hàm xử lý Modal, Translate, Delete  -->
    <script>
        ﻿        const poiDetailModal = document.getElementById('poiDetailModal');
        let currentPoiData = null;

        function openPoiModal(poi) {
            currentPoiData = poi;
            
            // Lấy thông tin cơ bản
            document.getElementById('modalName').textContent = poi.Name || poi.name || "Không tên";
            document.getElementById('modalAddress').innerHTML = `<i class="fas fa-map-marker-alt text-brand-500 mt-1"></i> <span>${poi.Address || poi.address || 'Đang cập nhật'}</span>`;
            document.getElementById('modalPhone').textContent = poi.Phone || poi.phone || "Không có SĐT";
            document.getElementById('modalRadius').textContent = parseInt(poi.triggerRadiusMeters || poi.triggerradiusmeters || poi.triggerRadiusmeters || 0);
            
            const isActive = parseInt(poi.IsActive ?? poi.isactive ?? poi.isActive ?? 0) === 1;
            document.getElementById('modalStatusBadge').innerHTML = isActive 
                ? `<span class="bg-green-500 text-white text-xs font-bold px-3 py-1.5 rounded-full uppercase tracking-wider shadow-sm flex items-center gap-1.5"><span class="w-1.5 h-1.5 bg-white rounded-full animate-pulse"></span> Hoạt động</span>`
                : `<span class="bg-gray-500 text-white text-xs font-bold px-3 py-1.5 rounded-full uppercase tracking-wider shadow-sm">Tạm ngưng</span>`;

            // Thống kê
            document.getElementById('modalVisits').textContent = poi.visitCount || poi.visitcount || 0;
            document.getElementById('modalAudioPlays').textContent = poi.audioPlayCount || poi.audioplaycount || 0;
            const avgDuration = parseFloat(poi.avgAudioDuration || poi.avgaudioduration || 0);
            document.getElementById('modalAvgDuration').textContent = isNaN(avgDuration) ? "0.0" : avgDuration.toFixed(1);

            // Mã QR
            const poiId = poi.Id || poi.id;
            const qrData = encodeURIComponent(`vinhkhanh://poi?id=${poiId}&action=play`);
            document.getElementById('modalQrCode').src = `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${qrData}`;

            // Đường dẫn map
            const mapLinkEl = document.getElementById('modalMapLink');
            const link = poi.MapLink || poi.maplink || poi.mapLink || '';
            if (link) {
                mapLinkEl.href = link;
                mapLinkEl.style.display = 'flex';
            } else {
                mapLinkEl.style.display = 'none';
            }

            // Xử lý hình ảnh
            let avatarUrl = poi.avatarUrl || poi.avatarurl || '';
            let bannerUrl = poi.bannerUrl || poi.bannerurl || '';

            // Nếu không có banner/avatar từ POIImage, fallback về ImageUrls
            if (!avatarUrl || !bannerUrl) {
                const rawImageUrls = poi.ImageUrls || poi.imageurls || '[]';
                try {
                    const parsed = JSON.parse(rawImageUrls);
                    if (Array.isArray(parsed)) {
                        if (!avatarUrl && parsed.length > 0) avatarUrl = parsed[0];
                        if (!bannerUrl && parsed.length > 1) bannerUrl = parsed[1];
                    }
                } catch (e) {
                    if (typeof rawImageUrls === 'string' && rawImageUrls.length > 5 && !avatarUrl) avatarUrl = rawImageUrls;
                }
            }

            if (avatarUrl) document.getElementById('modalAvatar').src = avatarUrl;
            if (bannerUrl) document.getElementById('modalBanner').src = bannerUrl;

            // Render ngôn ngữ
            renderLanguageTabs(poi);

            // Hiển thị modal
            poiDetailModal.classList.remove('hidden');
            poiDetailModal.classList.add('flex');
            
            // Animation
            requestAnimationFrame(() => {
                poiDetailModal.classList.remove('opacity-0', 'translate-y-4');
                poiDetailModal.classList.add('opacity-100', 'translate-y-0');
            });
        }

        function closePoiModal() {
            poiDetailModal.classList.remove('opacity-100', 'translate-y-0');
            poiDetailModal.classList.add('opacity-0', 'translate-y-4');
            setTimeout(() => {
                poiDetailModal.classList.remove('flex');
                poiDetailModal.classList.add('hidden');
                
                // Clear state
                activePopup = null;
                currentPoiData = null;
                document.getElementById('modalBanner').src = "";
                document.getElementById('modalAvatar').src = "";
            }, 300); // 300ms is the transition duration
        }

        // Đóng modal khi click ra ngoài vùng xám
        poiDetailModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closePoiModal();
            }
        });

        // Hàm render Tab và Content ngôn ngữ
        function renderLanguageTabs(poi) {
            const tabsEl = document.getElementById('langTabs');
            const contEl = document.getElementById('langContent');
            tabsEl.innerHTML = '';
            contEl.innerHTML = '';

            const languages = [
                { id: 'en', label: 'Anh', keyDesc: 'En', keyTts: 'En', flag: '🇬🇧' },
                { id: 'zh', label: 'Trung', keyDesc: 'Zh', keyTts: 'Zh', flag: '🇨🇳' },
                { id: 'ja', label: 'Nhật', keyDesc: 'Ja', keyTts: 'Ja', flag: '🇯🇵' },
                { id: 'ko', label: 'Hàn', keyDesc: 'Ko', keyTts: 'Ko', flag: '🇰🇷' },
                { id: 'fr', label: 'Pháp', keyDesc: 'Fr', keyTts: 'Fr', flag: '🇫🇷' },
                { id: 'ru', label: 'Nga', keyDesc: 'Ru', keyTts: 'Ru', flag: '🇷🇺' }
            ];

            // Tab Tiếng Việt (Luôn có)
            let hasAtLeastOneExtra = false;
            
            let tabsHtml = `<button onclick="switchTab('vi')" id="tab_vi" class="px-5 py-3.5 text-sm font-medium whitespace-nowrap border-b-2 bg-white border-brand-500 text-brand-600 focus:outline-none transition-colors w-1/3 md:w-auto text-center shrink-0">
                               🇻🇳 Tiếng Việt
                            </button>`;
                            
            const descVi = poi.DescriptionText || poi.descriptionText || poi.descriptiontext || '';
            const ttsVi = poi.TtsScript || poi.ttsScript || poi.ttsscript || '';
            
            let contentHtml = `<div id="content_vi" class="lang-content-panel block space-y-4">
                <div>
                    <h5 class="text-sm font-bold text-gray-700 mb-2 uppercase tracking-tight opacity-80"><i class="fas fa-align-left text-brand-400 mr-2"></i>Mô tả tóm tắt</h5>
                    <div class="bg-gray-50/50 p-4 rounded-xl text-gray-700 border border-gray-100 text-[15px] leading-relaxed">${descVi.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                </div>
                <div>
                    <div class="flex items-center justify-between mb-2">
                        <h5 class="text-sm font-bold text-gray-700 uppercase tracking-tight opacity-80"><i class="fas fa-headphones text-purple-400 mr-2"></i>Kịch bản TTS</h5>
                        ${(ttsVi.trim() || descVi.trim()) ? `<button data-text="${encodeURIComponent(ttsVi.trim() || descVi.trim())}" onclick="playTTS('vi', decodeURIComponent(this.getAttribute('data-text')))" class="text-xs bg-purple-100 text-purple-600 hover:bg-purple-200 px-3 py-1.5 rounded-full font-medium transition-colors flex items-center gap-1.5"><i class="fas fa-play"></i> Nghe</button>` : ''}
                    </div>
                    <div class="bg-purple-50/30 p-4 rounded-xl text-gray-700 border border-purple-100 text-[15px] leading-relaxed">${ttsVi.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                </div>
            </div>`;

            // Kiểm tra các ngôn ngữ khác
            languages.forEach(lang => {
                const descMapCased = 'description' + lang.keyDesc;
                const descPascal = 'Description' + lang.keyDesc;
                const descLower = descMapCased.toLowerCase();
                const descRaw = poi[descMapCased] || poi[descPascal] || poi[descLower] || '';
                
                const ttsMapCased = 'ttsScript' + lang.keyTts;
                const ttsPascal = 'TtsScript' + lang.keyTts;
                const ttsLower = ttsMapCased.toLowerCase();
                const ttsRaw = poi[ttsMapCased] || poi[ttsPascal] || poi[ttsLower] || '';

                if (descRaw.trim() !== '' || ttsRaw.trim() !== '') {
                    hasAtLeastOneExtra = true;
                    tabsHtml += `<button onclick="switchTab('${lang.id}')" id="tab_${lang.id}" class="px-5 py-3.5 text-sm font-medium whitespace-nowrap border-b-2 border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 focus:outline-none transition-colors w-1/3 md:w-auto text-center shrink-0">
                                   ${lang.flag} Tiếng ${lang.label}
                                </button>`;
                    
                    contentHtml += `<div id="content_${lang.id}" class="lang-content-panel hidden space-y-4">
                        <div>
                            <h5 class="text-sm font-bold text-gray-700 mb-2 uppercase tracking-tight opacity-80"><i class="fas fa-align-left text-brand-400 mr-2"></i>Mô tả tóm tắt</h5>
                            <div class="bg-gray-50/50 p-4 rounded-xl text-gray-700 border border-gray-100 text-[15px] leading-relaxed">${descRaw.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                        </div>
                        <div>
                            <div class="flex items-center justify-between mb-2">
                                <h5 class="text-sm font-bold text-gray-700 uppercase tracking-tight opacity-80"><i class="fas fa-headphones text-purple-400 mr-2"></i>Kịch bản TTS</h5>
                                ${(ttsRaw.trim() || descRaw.trim()) ? `<button data-text="${encodeURIComponent(ttsRaw.trim() || descRaw.trim())}" onclick="playTTS('${lang.id}', decodeURIComponent(this.getAttribute('data-text')))" class="text-xs bg-purple-100 text-purple-600 hover:bg-purple-200 px-3 py-1.5 rounded-full font-medium transition-colors flex items-center gap-1.5"><i class="fas fa-play"></i> Nghe</button>` : ''}
                            </div>
                            <div class="bg-purple-50/30 p-4 rounded-xl text-gray-700 border border-purple-100 text-[15px] leading-relaxed">${ttsRaw.replace(/\\n/g, '<br>') || '<i class="text-gray-400">Chưa cập nhật</i>'}</div>
                        </div>
                    </div>`;
                }
            });

            tabsEl.innerHTML = tabsHtml;
            contEl.innerHTML = contentHtml;
        }

        // Đổi tab ngôn ngữ
        window.switchTab = function(tabId) {
            // Reset all tabs
            document.querySelectorAll('#langTabs button').forEach(btn => {
                btn.classList.remove('bg-white', 'border-brand-500', 'text-brand-600');
                btn.classList.add('border-transparent', 'text-gray-500');
            });
            
            // Hide all content
            document.querySelectorAll('.lang-content-panel').forEach(panel => {
                panel.classList.add('hidden');
                panel.classList.remove('block');
            });

            // Active current tab
            const activeTab = document.getElementById('tab_' + tabId);
            if(activeTab) {
                activeTab.classList.remove('border-transparent', 'text-gray-500');
                activeTab.classList.add('bg-white', 'border-brand-500', 'text-brand-600');
            }

            // Show current content
            const activeContent = document.getElementById('content_' + tabId);
            if(activeContent) {
                activeContent.classList.remove('hidden');
                activeContent.classList.add('block');
            }
        };



        let addMap, editMap;
        let addMarker, editMarker;

        // Khởi tạo TrackAsia Map cho modal
        function initModalMaps() {
            // Map Add
            addMap = new trackasiagl.Map({
                container: 'add_map_canvas',
                style: 'https://maps.track-asia.com/styles/v2/streets.json?key=bca01773651908dcc9bc6320f7c16973ce',
                center: [106.702197, 10.761756], // Vĩnh Khánh mặc định
                zoom: 16
            });
            addMap.addControl(new trackasiagl.NavigationControl({showCompass: false}), 'top-right');
            addMarker = new trackasiagl.Marker({color: "#dc2626"})
                .setLngLat([106.702197, 10.761756])
                .addTo(addMap);
            addMap.on('click', (e) => {
                const lng = parseFloat(e.lngLat.lng.toFixed(6));
                const lat = parseFloat(e.lngLat.lat.toFixed(6));
                addMarker.setLngLat([lng, lat]);
                document.getElementById('add_longitude').value = lng;
                document.getElementById('add_latitude').value = lat;
            });

            // Map Edit
            editMap = new trackasiagl.Map({
                container: 'edit_map_canvas',
                style: 'https://maps.track-asia.com/styles/v2/streets.json?key=bca01773651908dcc9bc6320f7c16973ce',
                center: [106.702197, 10.761756],
                zoom: 16
            });
            editMap.addControl(new trackasiagl.NavigationControl({showCompass: false}), 'top-right');
            editMarker = new trackasiagl.Marker({color: "#dc2626"})
                .setLngLat([106.702197, 10.761756])
                .addTo(editMap);
            editMap.on('click', (e) => {
                const lng = parseFloat(e.lngLat.lng.toFixed(6));
                const lat = parseFloat(e.lngLat.lat.toFixed(6));
                editMarker.setLngLat([lng, lat]);
                document.getElementById('edit_longitude').value = lng;
                document.getElementById('edit_latitude').value = lat;
            });
        }
        
        // Gọi init lúc DOM ready
        window.addEventListener('DOMContentLoaded', () => {
            initModalMaps();
        });

        let currentAudioMap = null;

        // Cấu hình API Keys
        const ELEVEN_LABS_API_KEY = "sk_8db9fdb8efa165866e85ccc071c5ef803364bbd93324fe05";
        
        async function playTTS(lang, text) {
            if (!text) {
                alert("Không có nội dung để đọc.");
                return;
            }

            // Nếu đang đọc dở thì dừng
            stopTTS();

            // Phân loại Voice ID theo ngôn ngữ
            // Cả Tiếng Việt và các ngôn ngữ khác hiện đang dùng chung một voice ID của ElevenLabs (Multilingual)
            const voiceId = "EXAVITQu4vr4xnSDxMaL";

            try {
                // Hiển thị toast thông báo
                const toast = document.createElement('div');
                toast.id = 'tts_toast_loading';
                toast.className = 'fixed bottom-4 right-4 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-lg z-50 text-sm flex items-center gap-2';
                toast.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gọi ElevenLabs API...';
                document.body.appendChild(toast);

                const response = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`, {
                    method: 'POST',
                    headers: {
                        'xi-api-key': ELEVEN_LABS_API_KEY,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        text: text,
                        model_id: "eleven_multilingual_v2"
                    })
                });

                if (!response.ok) {
                    const errText = await response.text();
                    console.error("ElevenLabs Error:", errText);
                    document.getElementById('tts_toast_loading')?.remove();
                    alert("Lỗi kết nối ElevenLabs API: " + errText);
                    return;
                }

                // Nhận âm thanh dưới dạng blob (file binary)
                const audioBlob = await response.blob();
                const audioUrl = URL.createObjectURL(audioBlob);

                // Phát âm thanh
                currentAudioMap = new Audio(audioUrl);
                currentAudioMap.play();
                
                document.getElementById('tts_toast_loading').innerHTML = '<i class="fas fa-volume-up"></i> Đang đọc...';
                
                currentAudioMap.onended = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    URL.revokeObjectURL(audioUrl);
                };
                
                currentAudioMap.onerror = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    alert("Không thể phát file âm thanh từ ElevenLabs.");
                };

            } catch (err) {
                console.error('Fetch Error:', err);
                document.getElementById('tts_toast_loading')?.remove();
                alert("Lỗi gọi ElevenLabs API TTS.");
            }
        }

        function stopTTS() {
            // Dừng đối tượng âm thanh MP3
            if (currentAudioMap) {
                currentAudioMap.pause();
                currentAudioMap.currentTime = 0;
            }
            
            document.getElementById('tts_toast_loading')?.remove();

            // Đảm bảo dừng cả Web Speech API cũ (trường hợp bị xung đột)
            if ('speechSynthesis' in window) {
                window.speechSynthesis.cancel();
            }
        }

        function openEditModal(rawPoi) {
            const form = document.getElementById('editPoiForm');
            form.submitted = false; // Reset trạng thái submit
            // Chuyển toàn bộ keys về chữ thường để lấy data chính xác bất kể MySQL trả về kiểu gì
            const poi = {};
            for (let key in rawPoi) {
                poi[key.toLowerCase()] = rawPoi[key];
            }

            document.getElementById('editModal').classList.remove('hidden');
            
            document.getElementById('edit_id').value = poi.id || '';
            document.getElementById('edit_name').value = poi.name || '';
            document.getElementById('edit_latitude').value = poi.latitude || '0';
            document.getElementById('edit_longitude').value = poi.longitude || '0';
            document.getElementById('edit_address').value = poi.address || '';
            document.getElementById('edit_phone').value = poi.phone || '';
            document.getElementById('edit_triggerRadiusMeters').value = poi.triggerradiusmeters || poi.triggerradius || '20';
            document.getElementById('edit_mapLink').value = poi.maplink || '';
            document.getElementById('edit_isActive').value = poi.isactive !== undefined ? poi.isactive : '1';
            
            // Xử lý json ảnh trả ra field (nếu cần thiết)
            let imgs = poi.imageurls || '';
            document.getElementById('edit_imageUrls').value = imgs;

            // Nội dung & Thuyết minh
            document.getElementById('edit_descriptionText').value = poi.descriptiontext || '';
            document.getElementById('edit_descriptionEn').value = poi.descriptionen || '';
            document.getElementById('edit_descriptionZh').value = poi.descriptionzh || '';
            document.getElementById('edit_descriptionJa').value = poi.descriptionja || '';
            document.getElementById('edit_descriptionKo').value = poi.descriptionko || '';
            document.getElementById('edit_descriptionFr').value = poi.descriptionfr || '';
            document.getElementById('edit_descriptionRu').value = poi.descriptionru || '';
            
            document.getElementById('edit_ttsScript').value = poi.ttsscript || '';
            document.getElementById('edit_ttsScriptEn').value = poi.ttsscripten || '';
            document.getElementById('edit_ttsScriptZh').value = poi.ttsscriptzh || '';
            document.getElementById('edit_ttsScriptJa').value = poi.ttsscriptja || '';
            document.getElementById('edit_ttsScriptKo').value = poi.ttsscriptko || '';
            document.getElementById('edit_ttsScriptFr').value = poi.ttsscriptfr || '';
            document.getElementById('edit_ttsScriptRu').value = poi.ttsscriptru || '';

            // Kiểm tra xem đã có dữ liệu tiếng Anh / Trung chưa để hiển thị tương ứng
            if ((poi.descriptionen && poi.descriptionen.trim() !== '') || (poi.ttsscripten && poi.ttsscripten.trim() !== '')) {
                showEditLang('en');
            } else {
                hideEditLang('en');
            }

            if ((poi.descriptionzh && poi.descriptionzh.trim() !== '') || (poi.ttsscriptzh && poi.ttsscriptzh.trim() !== '')) {
                showEditLang('zh');
            } else {
                hideEditLang('zh');
            }

            if ((poi.descriptionja && poi.descriptionja.trim() !== '') || (poi.ttsscriptja && poi.ttsscriptja.trim() !== '')) {
                showEditLang('ja');
            } else {
                hideEditLang('ja');
            }

            if ((poi.descriptionko && poi.descriptionko.trim() !== '') || (poi.ttsscriptko && poi.ttsscriptko.trim() !== '')) {
                showEditLang('ko');
            } else {
                hideEditLang('ko');
            }

            if ((poi.descriptionfr && poi.descriptionfr.trim() !== '') || (poi.ttsscriptfr && poi.ttsscriptfr.trim() !== '')) {
                showEditLang('fr');
            } else {
                hideEditLang('fr');
            }

            if ((poi.descriptionru && poi.descriptionru.trim() !== '') || (poi.ttsscriptru && poi.ttsscriptru.trim() !== '')) {
                showEditLang('ru');
            } else {
                hideEditLang('ru');
            }

            // Prevent scrolling on body
            document.body.style.overflow = 'hidden';

            // Cập nhật bản đồ edit
            setTimeout(() => {
                if (editMap) {
                    editMap.resize();
                    const lat = parseFloat(poi.latitude) || 10.761756;
                    const lng = parseFloat(poi.longitude) || 106.702197;
                    editMap.setCenter([lng, lat]);
                    editMarker.setLngLat([lng, lat]);
                }
            }, 300);
        }

        function closeEditModal() {
            document.getElementById('editModal').classList.add('hidden');
            document.body.style.overflow = 'auto';
        }

        // Close on backdrop click
        document.getElementById('editModal').addEventListener('click', function(e) {
            if (e.target === this) {
                closeEditModal();
            }
        });

        // Tự động dịch bằng LibreTranslate API
        async function autoTranslate() {
            const descVi = document.getElementById('edit_descriptionText').value.trim();
            const ttsVi = document.getElementById('edit_ttsScript').value.trim();

            if (!descVi && !ttsVi) {
                alert("Vui lòng nhập nội dung Tiếng Việt trước khi dịch!");
                return;
            }

            const hasEn = document.getElementById('edit_has_en').value === '1';
            const hasZh = document.getElementById('edit_has_zh').value === '1';
            const hasJa = document.getElementById('edit_has_ja').value === '1';
            const hasKo = document.getElementById('edit_has_ko').value === '1';
            const hasFr = document.getElementById('edit_has_fr').value === '1';
            const hasRu = document.getElementById('edit_has_ru').value === '1';

            if (!hasEn && !hasZh && !hasJa && !hasKo && !hasFr && !hasRu) {
                alert("Vui lòng bật ít nhất một ngôn ngữ để dịch!");
                return;
            }

            const btnText = document.getElementById('translateBtnText');
            btnText.innerText = "Đang dịch...";
            
            try {
                // Hàm gọi API LibreTranslate đúng chuẩn (và API dự phòng nếu LibreTranslate đòi Key)
                const translateText = async (text, targetLang) => {
                    if (!text) return "";
                    
                    try {
                        const res = await fetch("https://libretranslate.com/translate", {
                            method: "POST",
                            body: JSON.stringify({
                                q: text,
                                source: "auto", // Đổi lại thành 'auto' như bạn yêu cầu
                                target: targetLang,
                                format: "text",
                                alternatives: 3,
                                api_key: ""
                            }),
                            headers: { "Content-Type": "application/json" }
                        });
                        
                        if (!res.ok) {
                            const errData = await res.json().catch(() => null);
                            console.error("LibreTranslate Error:", errData);
                            throw new Error("LibreTranslate API bị lỗi (thường do yêu cầu API Key).");
                        }
                        
                        const data = await res.json();
                        return data.translatedText || "";
                        
                    } catch (e) {
                        console.warn("Chuyển sang dùng Google Translate (Backup) vì LibreTranslate bị lỗi:", e.message);
                        
                        // API dự phòng (Google Translate Free) hoạt động 100% khi backend kia khóa
                        const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
                        const resBackup = await fetch(url);
                        const dataBackup = await resBackup.json();
                        return dataBackup[0].map(x => x[0]).join('');
                    }
                };

                const promises = [];
                
                if(descVi) {
                    if(hasEn) promises.push(translateText(descVi, "en").then(res => document.getElementById('edit_descriptionEn').value = res));
                    if(hasZh) promises.push(translateText(descVi, "zh").then(res => document.getElementById('edit_descriptionZh').value = res));
                    if(hasJa) promises.push(translateText(descVi, "ja").then(res => document.getElementById('edit_descriptionJa').value = res));
                    if(hasKo) promises.push(translateText(descVi, "ko").then(res => document.getElementById('edit_descriptionKo').value = res));
                    if(hasFr) promises.push(translateText(descVi, "fr").then(res => document.getElementById('edit_descriptionFr').value = res));
                    if(hasRu) promises.push(translateText(descVi, "ru").then(res => document.getElementById('edit_descriptionRu').value = res));
                }
                
                if(ttsVi) {
                    if(hasEn) promises.push(translateText(ttsVi, "en").then(res => document.getElementById('edit_ttsScriptEn').value = res));
                    if(hasZh) promises.push(translateText(ttsVi, "zh").then(res => document.getElementById('edit_ttsScriptZh').value = res));
                    if(hasJa) promises.push(translateText(ttsVi, "ja").then(res => document.getElementById('edit_ttsScriptJa').value = res));
                    if(hasKo) promises.push(translateText(ttsVi, "ko").then(res => document.getElementById('edit_ttsScriptKo').value = res));
                    if(hasFr) promises.push(translateText(ttsVi, "fr").then(res => document.getElementById('edit_ttsScriptFr').value = res));
                    if(hasRu) promises.push(translateText(ttsVi, "ru").then(res => document.getElementById('edit_ttsScriptRu').value = res));
                }

                await Promise.all(promises);

            } catch (error) {
                console.error(error);
                alert("Đã xảy ra lỗi khi dịch. Có thể do API giới hạn lượt gọi hoặc mạng có vấn đề.");
            } finally {
                btnText.innerText = "Dịch tự động";
            }
        }

        // Logic Modal Thêm Mới
        function openAddModal() {
            const form = document.getElementById('addPoiForm');
            form.reset();
            form.submitted = false; // Reset trạng thái submit
            ['en', 'zh', 'ja', 'ko', 'fr', 'ru'].forEach(lang => {
                document.getElementById('add_has_' + lang).value = '1';
                document.getElementById('add_' + lang + '_container').classList.remove('hidden');
            });
            document.getElementById('addModal').classList.remove('hidden');
            document.body.style.overflow = 'hidden';
            
            setTimeout(() => {
                if (addMap) {
                    addMap.resize();
                    const lat = parseFloat(document.getElementById('add_latitude').value) || 10.761756;
                    const lng = parseFloat(document.getElementById('add_longitude').value) || 106.702197;
                    addMap.setCenter([lng, lat]);
                    addMarker.setLngLat([lng, lat]);
                }
            }, 300);
        }

        function closeAddModal() {
            document.getElementById('addModal').classList.add('hidden');
            document.body.style.overflow = 'auto';
        }

        document.getElementById('addModal').addEventListener('click', function(e) {
            if (e.target === this) closeAddModal();
        });

        function showEditLang(lang) {
            document.getElementById('edit_' + lang + '_container').classList.remove('hidden');
            document.getElementById('btn_edit_' + lang).classList.add('hidden');
            document.getElementById('edit_has_' + lang).value = '1';
        }

        function hideEditLang(lang) {
            document.getElementById('edit_' + lang + '_container').classList.add('hidden');
            document.getElementById('btn_edit_' + lang).classList.remove('hidden');
            document.getElementById('edit_has_' + lang).value = '0';
        }
        
        async function autoTranslateAdd() {
            const descVi = document.getElementById('add_descriptionText').value.trim();
            const ttsVi = document.getElementById('add_ttsScript').value.trim();

            if (!descVi && !ttsVi) {
                alert("Vui lòng nhập nội dung Tiếng Việt trước khi dịch!");
                return;
            }

            const hasEn = document.getElementById('add_has_en').value === '1';
            const hasZh = document.getElementById('add_has_zh').value === '1';
            const hasJa = document.getElementById('add_has_ja').value === '1';
            const hasKo = document.getElementById('add_has_ko').value === '1';
            const hasFr = document.getElementById('add_has_fr').value === '1';
            const hasRu = document.getElementById('add_has_ru').value === '1';

            if (!hasEn && !hasZh && !hasJa && !hasKo && !hasFr && !hasRu) {
                alert("Vui lòng bật ít nhất một ngôn ngữ để dịch!");
                return;
            }

            const btnText = document.getElementById('translateBtnTextAdd');
            btnText.innerText = "Đang dịch...";
            
            try {
                // Tái sử dụng logic gọi API
                const translateText = async (text, targetLang) => {
                    if (!text) return "";
                    try {
                        const res = await fetch("https://libretranslate.com/translate", {
                            method: "POST",
                            body: JSON.stringify({
                                q: text,
                                source: "auto",
                                target: targetLang,
                                format: "text",
                                alternatives: 3,
                                api_key: ""
                            }),
                            headers: { "Content-Type": "application/json" }
                        });
                        
                        if (!res.ok) throw new Error("LibreTranslate Error");
                        const data = await res.json();
                        return data.translatedText || "";
                        
                    } catch (e) {
                        const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
                        const resBackup = await fetch(url);
                        const dataBackup = await resBackup.json();
                        return dataBackup[0].map(x => x[0]).join('');
                    }
                };

                const promises = [];
                if(descVi) {
                    if(hasEn) promises.push(translateText(descVi, "en").then(res => document.getElementById('add_descriptionEn').value = res));
                    if(hasZh) promises.push(translateText(descVi, "zh").then(res => document.getElementById('add_descriptionZh').value = res));
                    if(hasJa) promises.push(translateText(descVi, "ja").then(res => document.getElementById('add_descriptionJa').value = res));
                    if(hasKo) promises.push(translateText(descVi, "ko").then(res => document.getElementById('add_descriptionKo').value = res));
                    if(hasFr) promises.push(translateText(descVi, "fr").then(res => document.getElementById('add_descriptionFr').value = res));
                    if(hasRu) promises.push(translateText(descVi, "ru").then(res => document.getElementById('add_descriptionRu').value = res));
                }
                if(ttsVi) {
                    if(hasEn) promises.push(translateText(ttsVi, "en").then(res => document.getElementById('add_ttsScriptEn').value = res));
                    if(hasZh) promises.push(translateText(ttsVi, "zh").then(res => document.getElementById('add_ttsScriptZh').value = res));
                    if(hasJa) promises.push(translateText(ttsVi, "ja").then(res => document.getElementById('add_ttsScriptJa').value = res));
                    if(hasKo) promises.push(translateText(ttsVi, "ko").then(res => document.getElementById('add_ttsScriptKo').value = res));
                    if(hasFr) promises.push(translateText(ttsVi, "fr").then(res => document.getElementById('add_ttsScriptFr').value = res));
                    if(hasRu) promises.push(translateText(ttsVi, "ru").then(res => document.getElementById('add_ttsScriptRu').value = res));
                }

                await Promise.all(promises);
            } catch (error) {
                console.error(error);
                alert("Đã xảy ra lỗi khi dịch.");
            } finally {
                btnText.innerText = "Dịch tự động";
            }
        }
    </script>
</body>
</html>
