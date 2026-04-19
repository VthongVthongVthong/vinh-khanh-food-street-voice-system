<?php
session_start();
if (!isset($_SESSION['user_id']) || strtoupper($_SESSION['role']) !== 'ADMIN') {
    header("Location: login.php");
    exit;
}

$errorMsg = null;
$successMsg = null;

// Hàm upload ảnh lên Cloudinary (giống poi.php)
function uploadToCloudinary($tmpFile) {
    if (!$tmpFile || !file_exists($tmpFile)) return null;
    $cloudName = ''; // Để trống như trong poi.php
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

function fetchFirebaseData($path) {
    $url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" . $path;
    $ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, $url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
    curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
    $response = curl_exec($ch);
    curl_close($ch);
    
    if ($response && $response !== 'null') {
        return json_decode($response, true);
    }
    return [];
}

function putFirebaseData($path, $data) {
    $url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" . $path;
    $options = [
        "http" => [
            "method" => "PUT",
            "header" => "Content-Type: application/json",
            "content" => json_encode($data)
        ]
    ];
    $context = stream_context_create($options);
    @file_get_contents($url, false, $context);
}

function deleteFirebaseData($path) {
    $url = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" . $path;
    $options = [
        "http" => ["method" => "DELETE"]
    ];
    $context = stream_context_create($options);
    @file_get_contents($url, false, $context);
}

// Xử lý Form POST
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action'])) {
    $action = $_POST['action'];
    
    if ($action === 'delete_tour') {
        $id = (int)$_POST['id'];
        $tour = fetchFirebaseData("Tour/$id.json");
        if ($tour) {
            $tour['isActive'] = 0;
            putFirebaseData("Tour/$id.json", $tour);
            $successMsg = "Đã ẩn lộ trình thành công!";
        }
    } 
    else if ($action === 'save_tour') {
        $id = !empty($_POST['id']) ? (int)$_POST['id'] : null;
        $name = trim($_POST['name'] ?? '');
        $description = trim($_POST['description'] ?? '');
        $estimatedMinutes = (int)($_POST['estimatedMinutes'] ?? 0);
        $isActive = (int)($_POST['isActive'] ?? 1);
        
        $descriptionEn = trim($_POST['descriptionEn'] ?? '');
        $descriptionZh = trim($_POST['descriptionZh'] ?? '');
        $descriptionJa = trim($_POST['descriptionJa'] ?? '');
        $descriptionKo = trim($_POST['descriptionKo'] ?? '');
        $descriptionFr = trim($_POST['descriptionFr'] ?? '');
        $descriptionRu = trim($_POST['descriptionRu'] ?? '');
        
        $poiList = isset($_POST['poiList']) ? $_POST['poiList'] : [];

        // Upload Cover Image
        $coverImageUrl = $_POST['existingCover'] ?? '';
        if (isset($_FILES['coverImage']['tmp_name']) && $_FILES['coverImage']['error'] === UPLOAD_ERR_OK) {
            $uploaded = uploadToCloudinary($_FILES['coverImage']['tmp_name']);
            if ($uploaded) {
                $coverImageUrl = $uploaded;
            }
        }

        if (!$id) {
            // Find Max ID for Tour
            $allTours = fetchFirebaseData("Tour.json");
            $maxId = 0;
            if (is_array($allTours)) {
                foreach ($allTours as $key => $t) {
                    if ($t && isset($t['id']) && $t['id'] > $maxId) {
                        $maxId = (int)$t['id'];
                    }
                }
            }
            $id = $maxId + 1;
            $createdAt = date('Y-m-d\TH:i:s\Z');
        } else {
            // Fetch existing to preserve createdAt
            $existingTour = fetchFirebaseData("Tour/$id.json");
            $createdAt = $existingTour['createdAt'] ?? date('Y-m-d\TH:i:s\Z');
        }

        $tourData = [
            'id' => $id,
            'name' => $name,
            'description' => $description,
            'descriptionEn' => $descriptionEn,
            'descriptionZh' => $descriptionZh,
            'descriptionJa' => $descriptionJa,
            'descriptionKo' => $descriptionKo,
            'descriptionFr' => $descriptionFr,
            'descriptionRu' => $descriptionRu,
            'isActive' => $isActive,
            'estimatedMinutes' => $estimatedMinutes,
            'coverImageUrl' => $coverImageUrl,
            'createdAt' => $createdAt,
            'updatedAt' => date('Y-m-d\TH:i:s\Z')
        ];

        putFirebaseData("Tour/$id.json", $tourData);

        // Update TourPOI mapping
        // First delete existing TourPOIs for this tour
        $allTourPOIs = fetchFirebaseData("TourPOI.json");
        if (is_array($allTourPOIs)) {
            foreach ($allTourPOIs as $tpId => $tpData) {
                if ($tpData && isset($tpData['tourId']) && $tpData['tourId'] == $id) {
                    deleteFirebaseData("TourPOI/$tpId.json");
                }
            }
        }

        // Generate Max ID for TourPOI
        $allTourPOIs = fetchFirebaseData("TourPOI.json");
        $maxTpId = 0;
         if (is_array($allTourPOIs)) {
            foreach ($allTourPOIs as $tpId => $tpData) {
                if ($tpData && isset($tpData['id']) && $tpData['id'] > $maxTpId) {
                    $maxTpId = (int)$tpData['id'];
                }
            }
        }

        // Insert new selected POIs
        $sortOrder = 1;
        foreach ($poiList as $poiId) {
            if (empty($poiId)) continue;
            $maxTpId++;
            $tpData = [
                'id' => $maxTpId,
                'tourId' => $id,
                'poiId' => (int)$poiId,
                'sortOrder' => $sortOrder++
            ];
            putFirebaseData("TourPOI/$maxTpId.json", $tpData);
        }

        $successMsg = "Đã lưu thông tin lộ trình thành công!";
    }
}

// Lấy dữ liệu hiển thị
$toursData = fetchFirebaseData("Tour.json");
$allPoisData = fetchFirebaseData("POI.json");
$tourPoisData = fetchFirebaseData("TourPOI.json");
$tourLogsData = fetchFirebaseData("TourLog.json");

$tours = [];
if (is_array($toursData)) {
    foreach ($toursData as $t) {
        if ($t) $tours[] = $t;
    }
}
usort($tours, function($a, $b) {
    return ($b['id'] ?? 0) <=> ($a['id'] ?? 0);
});

// Calculate Stats from TourLog
$tourStats = [];
$totalParticipations = 0;
$totalCompleted = 0;
if (is_array($tourLogsData)) {
    foreach ($tourLogsData as $log) {
        if (!$log) continue;
        $totalParticipations++;
        $tId = $log['tourId'] ?? 0;
        if (!isset($tourStats[$tId])) {
            $tourStats[$tId] = ['visits' => 0, 'completed' => 0, 'avgCompletionRate' => 0, 'totalRate' => 0];
        }
        $tourStats[$tId]['visits']++;
        $rate = (float)($log['completionRate'] ?? 0);
        $tourStats[$tId]['totalRate'] += $rate;
        if ($rate >= 100 || ($log['status'] ?? '') === 'completed') {
            $tourStats[$tId]['completed']++;
            $totalCompleted++;
        }
    }
    
    foreach ($tourStats as $tId => &$stats) {
        if ($stats['visits'] > 0) {
            $stats['avgCompletionRate'] = round($stats['totalRate'] / $stats['visits'], 1);
        }
    }
}

$pois = [];
if (is_array($allPoisData)) {
    foreach ($allPoisData as $p) {
        if ($p) {
            $pid = $p['id'] ?? $p['Id'] ?? null;
            $pname = $p['name'] ?? $p['Name'] ?? null;
            if ($pid !== null && $pname !== null) {
                $pois[(int)$pid] = $pname;
            }
        }
    }
}

// Xử lý Tìm kiếm
$searchQuery = isset($_GET['search']) ? trim($_GET['search']) : '';
if ($searchQuery !== '') {
    $tours = array_filter($tours, function($t) use ($searchQuery) {
        return stripos($t['name'] ?? '', $searchQuery) !== false;
    });
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <link rel="icon" type="image/png" href="img/icon.png">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vĩnh Khánh CMS - Quản lý Lộ trình</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/sortablejs@latest/Sortable.min.js"></script>
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        primary: '#FF4D15',
                        brand: {
                            50: '#fff1ec',
                            100: '#ffdfd3',
                            500: '#FF4D15',
                            600: '#e63e00',
                        }
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
                    Vĩnh Khánh CMS
                </h1>
            </div>

            <!-- Navigation -->
            <nav class="p-4 space-y-1">
                <a href="index.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-th-large w-5 text-center"></i>
                    Tổng quan
                </a>
                <a href="poi.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-map-marker-alt w-5 text-center"></i>
                    Quản lý POI
                </a>
                <a href="map.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-map w-5 text-center"></i>
                    Bản đồ
                </a>
                <a href="tour.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
                    <i class="fas fa-route w-5 text-center"></i>
                    Quản lý Tour
                </a>
                <a href="heatmap.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-chart-area w-5 text-center"></i>
                    Phân tích Heatmap
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
            </div>
            
            <div class="flex items-center gap-4">
                <div class="flex items-center gap-3 cursor-pointer">
                    <div class="w-10 h-10 rounded-full bg-brand-100 text-brand-600 flex items-center justify-center font-bold">A</div>
                    <div class="hidden sm:block">
                        <p class="text-sm font-semibold text-gray-700">Admin</p>
                        <p class="text-xs text-gray-500">Quản trị viên</p>
                    </div>
                </div>
            </div>
        </header>

        <div class="p-6 md:p-8 w-full max-w-7xl mx-auto space-y-6">
            
            <!-- Page Header -->
            <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h2 class="text-2xl font-bold text-gray-800">Quản lý Lộ trình (Tour)</h2>
                    <p class="text-sm text-gray-500 mt-1">Danh sách các lộ trình dạo phố khám phá ẩm thực</p>
                </div>
                <button type="button" onclick="openAddModal()" class="bg-primary hover:bg-brand-600 text-white px-5 py-2.5 rounded-lg font-medium shadow-sm transition-all flex items-center gap-2 text-sm">
                    <i class="fas fa-plus"></i> Tạo Tour mới
                </button>
            </div>

            <?php if ($successMsg): ?>
            <div class="bg-green-50 text-green-600 p-4 rounded-lg flex items-center gap-3">
                <i class="fas fa-check-circle"></i>
                <?php echo $successMsg; ?>
            </div>
            <?php endif; ?>

            <!-- Analytics Overview -->
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
                <!-- Stat Card 1 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Mức độ hoàn thành Tour (TB)</p>
                            <h3 class="text-2xl font-bold text-gray-800">
                                <?php echo $totalParticipations > 0 ? round(($totalCompleted / $totalParticipations) * 100, 1) : 0; ?>%
                            </h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-chart-line"></i>
                        </div>
                    </div>
                </div>

                <!-- Stat Card 2 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Lượt tham gia Tour</p>
                            <h3 class="text-2xl font-bold text-gray-800"><?php echo number_format($totalParticipations, 0, ',', '.'); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center">
                            <i class="fas fa-users"></i>
                        </div>
                    </div>
                </div>

                <!-- Stat Card 3 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tour đang hoạt động</p>
                            <h3 class="text-2xl font-bold text-gray-800"><?php echo count(array_filter($tours, fn($t) => ($t['isActive']??0) == 1)); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-red-50 text-red-500 flex items-center justify-center">
                            <i class="fas fa-route"></i>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Table -->
            <div class="bg-white border border-gray-200 rounded-xl shadow-sm overflow-visible">
                <div class="overflow-visible">
                    <table class="w-full text-left border-collapse">
                        <thead>
                            <tr class="bg-gray-50/50 text-gray-500 text-sm border-b border-gray-100">
                                <th class="font-medium p-4 pl-6">Hình & Tên Lộ Trình</th>
                                <th class="font-medium p-4">Danh sách POI (Theo Thứ tự)</th>
                                <th class="font-medium p-4">Thời lượng (Phút)</th>
                                <th class="font-medium p-4">Phân tích lượt đi</th>
                                <th class="font-medium p-4">Trạng thái</th>
                                <th class="font-medium p-4 pr-6 text-right">Thao tác</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-gray-100 text-sm">
                            <?php foreach ($tours as $tour): 
                                $tId = $tour['id'] ?? 0;
                                $name = htmlspecialchars($tour['name'] ?? 'Không tên');
                                $desc = htmlspecialchars($tour['description'] ?? '');
                                $coverImage = $tour['coverImageUrl'] ?? '';
                                $status = (int)($tour['isActive'] ?? 0);
                                $estMin = (int)($tour['estimatedMinutes'] ?? 0);
                                
                                // Tìm POI của tour
                                $thisTourPoisList = [];
                                if (is_array($tourPoisData)) {
                                    $filtered = array_filter($tourPoisData, fn($tp) => ($tp['tourId'] ?? 0) == $tId);
                                    usort($filtered, fn($a, $b) => ($a['sortOrder'] ?? 0) <=> ($b['sortOrder'] ?? 0));
                                    foreach ($filtered as $ftp) {
                                        $thisTourPoisList[] = $ftp['poiId'] ?? 0;
                                    }
                                }

                                // Tỉ lệ hoàn thành
                                $visits = $tourStats[$tId]['visits'] ?? 0;
                                $avgRate = $tourStats[$tId]['avgCompletionRate'] ?? 0;
                            ?>
                            <tr class="hover:bg-gray-50/50 transition-colors group">
                                <td class="p-4 pl-6">
                                    <div class="flex items-center gap-3">
                                        <div class="w-16 h-12 rounded overflow-hidden flex-shrink-0 bg-gray-200 cursor-pointer" onclick='javascript:openEditModal(<?php echo htmlspecialchars(json_encode([
                                            'tour' => $tour,
                                            'pois' => $thisTourPoisList
                                        ], JSON_UNESCAPED_UNICODE), ENT_QUOTES, "UTF-8"); ?>)'>
                                            <?php if (!empty($coverImage)): ?>
                                                <img src="<?php echo htmlspecialchars($coverImage); ?>" class="w-full h-full object-cover">
                                            <?php else: ?>
                                                <img src="https://ui-avatars.com/api/?name=<?php echo urlencode($name); ?>&background=E5E7EB&color=9CA3AF" class="w-full h-full object-cover">
                                            <?php endif; ?>
                                        </div>
                                        <div>
                                            <div class="font-bold text-gray-800 text-base"><?php echo $name; ?></div>
                                            <div class="text-xs text-gray-500 mt-0.5"><?php echo (strlen($desc) > 35) ? substr($desc,0,35).'...' : $desc; ?></div>
                                        </div>
                                    </div>
                                </td>
                                <td class="p-4">
                                    <div class="flex flex-col gap-1 text-xs text-gray-600">
                                        <?php if (empty($thisTourPoisList)): ?>
                                            <span class="text-red-400">Chưa có POI nào</span>
                                        <?php else: ?>
                                            <b><?php echo count($thisTourPoisList); ?> POI (Có thứ tự):</b>
                                            <?php 
                                            $showPois = array_slice($thisTourPoisList, 0, 3);
                                            foreach($showPois as $idx => $pid) {
                                                echo "<span>".($idx+1).". " . htmlspecialchars($pois[$pid] ?? "POI $pid") . "</span>";
                                            }
                                            if (count($thisTourPoisList) > 3) echo "<span class='text-brand-500 mt-1 font-medium'>+ " . (count($thisTourPoisList)-3) . " địa điểm khác...</span>";
                                            ?>
                                        <?php endif; ?>
                                    </div>
                                </td>
                                <td class="p-4 font-medium"><?php echo $estMin; ?> phút</td>
                                <td class="p-4">
                                    <div class="text-gray-700 font-medium"><?php echo $visits; ?> lượt tham gia</div>
                                    <div class="text-xs <?php echo $avgRate > 50 ? 'text-green-500' : 'text-yellow-600'; ?> mt-1">TB Hoàn thành: <?php echo $avgRate; ?>%</div>
                                </td>
                                <td class="p-4">
                                    <?php if ($status === 1): ?>
                                        <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-green-50 text-green-700 text-xs font-medium border border-green-200"><span class="w-1.5 h-1.5 rounded-full bg-green-500"></span>Hoạt động</span>
                                    <?php else: ?>
                                        <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-gray-100 text-gray-600 text-xs font-medium border border-gray-200"><span class="w-1.5 h-1.5 rounded-full bg-gray-400"></span>Tạm ngưng</span>
                                    <?php endif; ?>
                                </td>
                                <td class="p-4 pr-6 text-right">
                                    <div class="flex items-center justify-end gap-2">
                                        <button onclick='javascript:openEditModal(<?php echo htmlspecialchars(json_encode([
                                            'tour' => $tour,
                                            'pois' => $thisTourPoisList
                                        ], JSON_UNESCAPED_UNICODE), ENT_QUOTES, "UTF-8"); ?>)' class="p-2 text-gray-400 hover:text-brand-600 hover:bg-brand-50 rounded-lg transition-colors" title="Sửa">
                                            <i class="fas fa-edit"></i>
                                        </button>
                                        <form method="POST" class="inline" onsubmit="return confirm('Bạn có chắc chắn muốn ẩn lộ trình này không?');">
                                            <input type="hidden" name="action" value="delete_tour">
                                            <input type="hidden" name="id" value="<?php echo $tId; ?>">
                                            <button type="submit" class="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors" title="Ẩn Lộ trình">
                                                <i class="fas fa-ban"></i>
                                            </button>
                                        </form>
                                    </div>
                                </td>
                            </tr>
                            <?php endforeach; ?>
                            <?php if (empty($tours)): ?>
                            <tr><td colspan="6" class="p-8 text-center text-gray-400">Chưa có lộ trình nào. Hãy bấm mục "Tạo Tour mới" để tạo một Lộ trình!</td></tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </main>

    <!-- Modal Form -->
    <div id="tourModal" class="fixed inset-0 z-50 hidden">
        <div class="absolute inset-0 bg-gray-900/50 backdrop-blur-sm" onclick="closeModal()"></div>
        
        <div class="absolute inset-y-0 right-0 w-full md:w-[600px] bg-white shadow-2xl flex flex-col transform transition-transform duration-300 translate-x-full" id="tourModalContent">
            
            <div class="px-6 py-4 border-b border-gray-100 flex items-center justify-between bg-white shrink-0">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-lg bg-brand-50 text-brand-600 flex items-center justify-center">
                        <i class="fas fa-route"></i>
                    </div>
                    <div>
                        <h3 class="text-lg font-bold text-gray-800" id="modalTitle">Thêm Lộ Trình Mới</h3>
                        <p class="text-sm text-gray-500">Điền thông tin chi tiết cho lộ trình</p>
                    </div>
                </div>
                <button onclick="closeModal()" class="text-gray-400 hover:text-gray-600 p-2 rounded-lg hover:bg-gray-50 transition-colors">
                    <i class="fas fa-times text-xl"></i>
                </button>
            </div>

            <div class="flex-1 overflow-y-auto bg-gray-50/50">
                <form id="tourForm" method="POST" action="tour.php" enctype="multipart/form-data" class="p-6 space-y-6">
                    <input type="hidden" name="action" value="save_tour">
                    <input type="hidden" id="tourId" name="id" value="">
                    
                    <div class="bg-white p-5 rounded-xl border border-gray-200">
                        <label class="block text-sm font-semibold text-gray-700 mb-1">Tên lộ trình <span class="text-red-500">*</span></label>
                        <input type="text" id="name" name="name" required class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all font-medium">
                    </div>

                    <div class="bg-white p-5 rounded-xl border border-gray-200">
                        <div class="flex gap-4">
                            <div class="w-32 hidden" id="qrContainer">
                                <label class="block text-sm font-semibold text-gray-700 mb-1 text-center">Mã QR</label>
                                <div class="border border-gray-200 rounded p-1 shadow-sm bg-white">
                                    <img src="" id="tourQrCode" class="w-full h-auto object-contain" alt="Mã QR" />
                                </div>
                                <div class="text-[10px] text-gray-500 text-center mt-1 w-full font-medium">Lưu hình hoặc quét</div>
                            </div>
                            <div class="flex-1">
                                <label class="block text-sm font-semibold text-gray-700 mb-1">Ảnh bìa (Cover Image)</label>
                                <input type="file" id="coverImage" name="coverImage" accept="image/*" class="w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100">
                                <input type="hidden" id="existingCover" name="existingCover" value="">
                                <div id="coverPreview" class="mt-3 aspect-video bg-gray-100 rounded-lg overflow-hidden hidden">
                                    <img id="coverImgTag" src="" class="w-full h-full object-cover">
                                </div>
                            </div>
                            <div class="flex-1">
                                <label class="block text-sm font-semibold text-gray-700 mb-1">Thời gian ước tính (phút)</label>
                                <input type="number" id="estimatedMinutes" name="estimatedMinutes" class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:border-brand-500">
                                
                                <label class="block text-sm font-semibold text-gray-700 mb-1 mt-4">Trạng thái</label>
                                <select id="isActive" name="isActive" class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:border-brand-500">
                                    <option value="1">Hoạt động</option>
                                    <option value="0">Tạm ngưng</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    <div class="bg-white p-5 rounded-xl border border-gray-200">
                        <div class="flex items-center justify-between mb-3">
                            <label class="block text-sm font-semibold text-gray-700">Mô tả (Mặc định) <span class="text-red-500">*</span></label>
                            <button type="button" id="btnTranslate" onclick="autoTranslate()" class="text-xs bg-blue-50 text-blue-600 hover:bg-blue-100 px-3 py-1.5 rounded-lg font-medium transition-colors border border-blue-100 flex items-center gap-1.5">
                                <i class="fas fa-language"></i> Dịch tự động ra các thứ tiếng
                            </button>
                        </div>
                        <textarea id="description" name="description" rows="3" required class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:border-brand-500"></textarea>

                        <div class="mt-4 space-y-4" id="langContainer">
                            <div class="flex items-center gap-4 border-b pb-2 mt-6">
                                <span class="text-sm font-semibold">Mô tả đa ngôn ngữ</span>
                            </div>
                            <!-- All languages always visible -->
                            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Anh (English)</label>
                                    <textarea name="descriptionEn" id="descriptionEn" placeholder="Mô tả tiếng Anh..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Trung (中文)</label>
                                    <textarea name="descriptionZh" id="descriptionZh" placeholder="Mô tả tiếng Trung..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Nhật (日本語)</label>
                                    <textarea name="descriptionJa" id="descriptionJa" placeholder="Mô tả tiếng Nhật..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Hàn (한국어)</label>
                                    <textarea name="descriptionKo" id="descriptionKo" placeholder="Mô tả tiếng Hàn..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Pháp (Français)</label>
                                    <textarea name="descriptionFr" id="descriptionFr" placeholder="Mô tả tiếng Pháp..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                                <div>
                                    <label class="text-xs font-semibold text-gray-600 mb-1 block">Tiếng Nga (Русский)</label>
                                    <textarea name="descriptionRu" id="descriptionRu" placeholder="Mô tả tiếng Nga..." class="w-full p-2 bg-gray-50 border border-gray-200 rounded focus:border-brand-500 text-sm transition-colors duration-300"></textarea>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="bg-white p-5 rounded-xl border border-gray-200">
                        <label class="block text-sm font-semibold text-gray-700 mb-3">Danh sách Địa Điểm (POI) tham gia lộ trình</label>
                        <p class="text-xs text-brand-600 mb-4 bg-brand-50 p-3 rounded leading-relaxed border border-brand-100 font-medium whitespace-normal"><i class="fas fa-info-circle mr-1"></i>Tích chọn các địa điểm tham gia Lộ trình này. <br><i class="fas fa-arrows-alt-v mr-1"></i>Sau đó sử dụng thao tác <b>kéo thả vùng trắng</b> để sắp xếp chuẩn thứ tự ưu tiên (Sort Order).</p>
                        
                        <div id="poiSortableList" class="grid grid-cols-1 gap-2 max-h-60 overflow-y-auto p-2 border rounded bg-gray-50/50">
                            <?php foreach ($pois as $pid => $pname): ?>
                            <div class="poi-item flex items-center justify-between gap-3 p-3 bg-white hover:bg-gray-50 border shadow-sm rounded cursor-move transition-shadow">
                                <label class="flex items-center gap-3 cursor-pointer flex-1">
                                    <input type="checkbox" name="poiList[]" value="<?php echo $pid; ?>" class="poi-checkbox w-4 h-4 text-brand-600 border-gray-300 rounded focus:ring-brand-500">
                                    <span class="text-sm font-medium text-gray-800"><?php echo htmlspecialchars($pname); ?></span>
                                </label>
                                <i class="fas fa-grip-lines text-gray-400 cursor-move" title="Kéo để sắp xếp"></i>
                            </div>
                            <?php endforeach; ?>
                        </div>
                    </div>

                </form>
            </div>

            <div class="px-6 py-4 border-t border-gray-100 bg-white flex justify-end gap-3 shrink-0">
                <button type="button" onclick="closeModal()" class="px-5 py-2.5 text-gray-600 bg-gray-100 hover:bg-gray-200 rounded-lg font-medium transition-colors">
                    Hủy
                </button>
                <button type="button" onclick="submitForm()" class="px-5 py-2.5 bg-brand-500 hover:bg-brand-600 text-white rounded-lg font-medium shadow-sm transition-colors flex items-center gap-2">
                    <i class="fas fa-save"></i>
                    Lưu thông tin
                </button>
            </div>
        </div>
    </div>

    <script>
        async function autoTranslate() {
            const text = document.getElementById('description').value;
            if (!text.trim()) {
                alert("Vui lòng nhập mô tả mặc định (Tiếng Việt) trước khi dịch.");
                return;
            }
            
            const btn = document.getElementById('btnTranslate');
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang dịch...';
            btn.disabled = true;

            const langs = {
                'En': 'en',
                'Zh': 'zh-CN',
                'Ja': 'ja',
                'Ko': 'ko',
                'Fr': 'fr',
                'Ru': 'ru'
            };

            try {
                for (const [ucLang, glang] of Object.entries(langs)) {
                    const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${glang}&dt=t&q=${encodeURIComponent(text)}`;
                    const response = await fetch(url);
                    const data = await response.json();
                    
                    let translatedText = '';
                    if (data && data[0]) {
                        data[0].forEach(item => {
                            if (item[0]) translatedText += item[0];
                        });
                    }
                    
                    if (translatedText) {
                        const targetArea = document.getElementById('description' + ucLang);
                        if (targetArea) {
                            targetArea.value = translatedText;
                            targetArea.classList.add('bg-green-50');
                            setTimeout(() => targetArea.classList.remove('bg-green-50'), 1000);
                        }
                    }
                }
            } catch (e) {
                alert('Có lỗi xảy ra khi dịch: ' + e.message);
            } finally {
                btn.innerHTML = '<i class="fas fa-language"></i> Dịch tự động ra các thứ tiếng';
                btn.disabled = false;
            }
        }

        function openAddModal() {
            document.getElementById('tourForm').reset();
            document.getElementById('tourId').value = '';
            document.getElementById('modalTitle').innerText = 'Thêm Lộ Trình Mới';
            
            
            document.getElementById('coverPreview').classList.add('hidden');
            document.getElementById('coverImgTag').src = '';
            document.getElementById('existingCover').value = '';
            document.getElementById('qrContainer').classList.add('hidden');

            const checkboxes = document.querySelectorAll('.poi-checkbox');
            checkboxes.forEach(cb => cb.checked = false);
            
            // Xếp lại tất cả mục poi-item
            const container = document.getElementById('poiSortableList');
            const items = Array.from(container.querySelectorAll('.poi-item'));
            items.forEach(el => container.appendChild(el));

            showModal();
        }

        function openEditModal(payload) {
            document.getElementById('tourForm').reset();
            
            const tour = payload.tour;
            const selectedPois = payload.pois || [];

            document.getElementById('tourId').value = tour.id || '';
            document.getElementById('name').value = tour.name || '';
            document.getElementById('description').value = tour.description || '';
            document.getElementById('estimatedMinutes').value = tour.estimatedMinutes || '0';
            document.getElementById('isActive').value = tour.isActive !== undefined ? tour.isActive : '1';
            
            document.getElementById('existingCover').value = tour.coverImageUrl || '';
            if (tour.coverImageUrl) {
                document.getElementById('coverPreview').classList.remove('hidden');
                document.getElementById('coverImgTag').src = tour.coverImageUrl;
            } else {
                document.getElementById('coverPreview').classList.add('hidden');
                document.getElementById('coverImgTag').src = '';
            }

            if (tour.id) {
                const qrData = encodeURIComponent(`vinhkhanh://tour?id=${tour.id}&action=play`);
                document.getElementById('tourQrCode').src = `https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${qrData}`;
                document.getElementById('qrContainer').classList.remove('hidden');
                document.getElementById('qrContainer').classList.add('block');
            } else {
                document.getElementById('qrContainer').classList.add('hidden');
                document.getElementById('qrContainer').classList.remove('block');
            }

            const langs = ['en','zh','ja','ko','fr','ru'];
            langs.forEach(lang => {
                const ucLang = lang.charAt(0).toUpperCase() + lang.slice(1);
                const desc = tour['description' + ucLang];
                document.getElementById('description'+ucLang).value = desc || '';
            });

            // Map selected POIs and sort DOM
            const container = document.getElementById('poiSortableList');
            const items = Array.from(container.querySelectorAll('.poi-item'));
            
            // Uncheck all first
            items.forEach(el => {
                el.querySelector('input.poi-checkbox').checked = false;
            });

            // Append sorted selected items at the top
            selectedPois.forEach(pid => {
                const el = items.find(item => item.querySelector('input.poi-checkbox').value == pid);
                if (el) {
                    el.querySelector('input.poi-checkbox').checked = true;
                    container.appendChild(el);
                }
            });

            // Append unchecked items at the bottom
            items.forEach(el => {
                if (!el.querySelector('input.poi-checkbox').checked) {
                    container.appendChild(el);
                }
            });

            document.getElementById('modalTitle').innerText = 'Sửa Lộ Trình: ' + tour.name;
            showModal();
        }

        function showModal() {
            const modal = document.getElementById('tourModal');
            const content = document.getElementById('tourModalContent');
            modal.classList.remove('hidden');
            setTimeout(() => {
                content.classList.remove('translate-x-full');
            }, 10);
        }

        function closeModal() {
            const modal = document.getElementById('tourModal');
            const content = document.getElementById('tourModalContent');
            content.classList.add('translate-x-full');
            setTimeout(() => {
                modal.classList.add('hidden');
            }, 300);
        }

        function submitForm() {
            document.getElementById('tourForm').submit();
        }

        // Init SortableJS
        document.addEventListener('DOMContentLoaded', function() {
            const el = document.getElementById('poiSortableList');
            new Sortable(el, {
                animation: 150,
                ghostClass: 'bg-blue-50'
            });
        });
    </script>
</body>
</html>
