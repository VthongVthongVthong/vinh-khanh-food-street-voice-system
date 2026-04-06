<?php
header("Access-Control-Allow-Origin: *");
header("Content-Type: application/json; charset=UTF-8");
require_once "db.php";

$db = new SQLiteDB();
$pdo = $db->getPDO();

try {
    $stmt = $pdo->query("
        SELECT    
        id,
        name,
        latitude,
        longitude,
        address,
        phone,
        descriptionText,
        ttsScript,
        imageUrls,
        mapLink,
        triggerRadiusMeters,
        isActive,
        createdAt,
        updatedAt,
        ownerId,
        descriptionEn,
        descriptionZh,
        ttsScriptEn,
        ttsScriptZh,
        descriptionJa,
        descriptionKo,
        descriptionFr,
        descriptionRu,
        ttsScriptJa,
        ttsScriptRu,
        ttsScriptKo,
        ttsScriptFr
        FROM POI
        WHERE isActive = 1
    ");

    $data = $stmt->fetchAll(PDO::FETCH_ASSOC);

    echo json_encode([
        "status" => "success",
        "data" => $data
    ], JSON_UNESCAPED_UNICODE);
} catch (Exception $e) {
    echo json_encode([
        "status" => "error",
        "message" => $e->getMessage()
    ], JSON_UNESCAPED_UNICODE);
}
?>