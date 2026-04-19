<?php
session_start();
if (!isset($_SESSION['user_id']) || strtoupper($_SESSION['role']) !== 'ADMIN') {
    header("Location: login.php");
    exit;
}
require_once 'db.php';
$db = new SQLiteDB();
$pdo = $db->getPDO();

$pois = [];
try {
    $stmt = $pdo->query("
        SELECT 
            p.*, 
            (SELECT imageUrl FROM POIImage WHERE poiId = p.Id AND imageType = 'avatar' LIMIT 1) as avatarUrl,
            (SELECT imageUrl FROM POIImage WHERE poiId = p.Id AND imageType = 'banner' LIMIT 1) as bannerUrl,
            (SELECT COUNT(id) FROM VisitLog WHERE poiId = p.Id) as visitCount,
            (SELECT COUNT(id) FROM AudioPlayLog WHERE poiId = p.Id) as audioPlayCount,
            (SELECT AVG(durationListened) FROM AudioPlayLog WHERE poiId = p.Id) as avgAudioDuration
        FROM POI p
    ");
    $pois = $stmt->fetchAll(PDO::FETCH_ASSOC);
} catch (Exception $e) {
    // Log error or ignore if table doesn't exist
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <link rel="icon" type="image/png" href="img/icon.png">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vĩnh Khánh CMS - Bản đồ</title>
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
    <style>
        /* Custom TrackAsia Popup classes for clean look */
        .trackasiagl-popup-content {
            border-radius: 12px;
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
            padding: 12px;
            border: 1px solid #f3f4f6;
        }
        .trackasiagl-popup-close-button {
            right: 8px;
            top: 6px;
            color: #9ca3af;
            font-size: 18px;
        }
        .trackasiagl-popup-close-button:hover {
            background-color: transparent;
            color: #dc2626;
        }

        /* Hiệu ứng HOT / Đang Trending */
        .poi-fire > div {
            box-shadow: 0 0 15px 5px rgba(255,87,34,0.8), 0 0 30px 10px rgba(255,152,0,0.6) !important;
            border: 2px solid #ffeb3b !important;
            animation: pulse-fire 1s infinite alternate;
        }

        @keyframes pulse-fire {
            0% { box-shadow: 0 0 15px 5px rgba(255,87,34,0.7), 0 0 30px 10px rgba(255,152,0,0.5); }
            100% { box-shadow: 0 0 25px 10px rgba(255,87,34,1), 0 0 50px 20px rgba(255,152,0,0.8); }
        }
    </style>
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
                <a href="map.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
                    <i class="fas fa-map w-5 text-center"></i>
                    Bản đồ
                </a>
                <a href="tour.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
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
                <div class="relative w-full max-w-md hidden sm:block">
                    <i class="fas fa-search absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"></i>
                    <input type="text" id="searchInput" placeholder="Tìm kiếm quán theo tên..." class="w-full pl-10 pr-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all text-sm" autocomplete="off">
                    <div id="searchResults" class="absolute top-full left-0 right-0 mt-1 bg-white border border-gray-200 rounded-lg shadow-lg z-50 max-h-60 overflow-y-auto hidden"></div>
                </div>
            </div>

            <div class="flex items-center gap-4">
                <button class="relative p-2 text-gray-400 hover:text-gray-600 transition-colors">
                    <i class="far fa-bell text-xl"></i>
                    <span class="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full border-2 border-white"></span>
                </button>
                <div class="flex items-center gap-3 pl-4 border-l border-gray-200">
                    <div class="w-8 h-8 rounded-full border-2 border-primary/20 flex items-center justify-center bg-brand-50 text-brand-600">
                        <i class="far fa-user"></i>
                    </div>
                    <div class="hidden sm:block">
                        <div class="text-sm font-semibold text-gray-800">Admin</div>
                        <div class="text-xs text-gray-500">Quản trị viên</div>
                    </div>
                </div>
            </div>
        </header>

        <!-- Map Container -->
        <div class="relative w-full h-[calc(100vh-4rem)] p-4 md:p-6 bg-gray-50">
            <div class="w-full h-full bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden relative">
                <div id="map" class="w-full h-full"></div>
                
                <!-- Detail Modal (Hidden by default) -->
                <div id="poiDetailModal" class="absolute inset-0 bg-white/95 backdrop-blur-sm z-20 hidden flex-col transition-all duration-300 opacity-0 translate-y-4 shadow-2xl overflow-y-auto">
                    <div class="sticky top-0 w-full flex justify-between items-center px-6 py-4 bg-white/90 backdrop-blur z-30 border-b border-gray-100 shadow-sm">
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

                <!-- Legend overlay -->
                <div class="absolute top-6 right-6 bg-white/95 backdrop-blur-md rounded-xl shadow-xl border border-gray-100 p-5 z-10 w-56 md:w-64">
                    <h4 class="font-bold text-gray-800 mb-4 text-sm tracking-wide">Chú giải Geofence</h4>
                    <div class="space-y-3">
                        <div class="flex items-center gap-3 text-sm text-gray-600">
                            <span class="w-3.5 h-3.5 rounded-full bg-primary shadow-sm shadow-brand-500/50 flex-shrink-0"></span> 
                            <span>Điểm đang hoạt động</span>
                        </div>
                        <div class="flex items-center gap-3 text-sm text-gray-600">
                            <span class="w-3.5 h-3.5 rounded-full bg-gray-500 shadow-sm shadow-gray-400/50 flex-shrink-0"></span> 
                            <span>Điểm tạm ngưng</span>
                        </div>
                        <div class="flex items-center gap-3 text-sm text-gray-600">
                            <span class="w-3.5 h-3.5 rounded-full border-[2.5px] border-primary/60 bg-transparent flex-shrink-0"></span> 
                            <span>Vùng kích hoạt (Radius)</span>
                        </div>
                    </div>
                    
                    <div class="mt-4 pt-4 border-t border-gray-100 flex items-center justify-between">
                        <span class="text-[13px] font-medium text-gray-700">Hiện POI tạm ngưng</span>
                        <label class="relative inline-flex items-center cursor-pointer">
                            <input type="checkbox" id="toggleInactivePOI" class="sr-only peer">
                            <div class="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-brand-500"></div>
                        </label>
                    </div>
                    
                    <div class="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
                        <span class="text-[13px] font-medium text-gray-700">Hiện Heatmap tương tác</span>
                        <label class="relative inline-flex items-center cursor-pointer">
                            <input type="checkbox" id="toggleHeatmap" class="sr-only peer" checked>
                            <div class="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-red-500"></div>
                        </label>
                    </div>
                    <div class="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
                        <span class="text-[13px] font-medium text-gray-700">Lọc theo khung giờ</span>
                        <label class="relative inline-flex items-center cursor-pointer">
                            <input type="checkbox" id="toggleTimeFilter" class="sr-only peer">
                            <div class="w-9 h-5 bg-gray-200 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-brand-500"></div>
                        </label>
                    </div>
                </div>

                <!-- Time Slider overlay -->
                <div class="absolute bottom-10 left-1/2 -translate-x-1/2 bg-white/95 backdrop-blur-md rounded-2xl shadow-[0_10px_25px_rgba(0,0,0,0.1)] border border-gray-100 p-4 z-10 w-[90%] max-w-sm flex-col gap-3 transition-opacity transition-transform" id="timeSliderContainer" style="display: none;">
                    <div class="flex justify-between items-center text-sm font-bold text-gray-700">
                        <span><i class="far fa-clock text-brand-500 mr-2"></i>Lọc theo Khung Giờ</span>
                        <span id="timeSliderVal" class="text-brand-600 bg-brand-50 px-2.5 py-1 rounded-md text-xs font-mono">18:00 - 18:59</span>
                    </div>
                    <input type="range" id="timeSlider" min="16" max="23" value="18" step="1" 
                           class="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer">
                    <div class="flex justify-between text-[11px] text-gray-400 font-medium pt-1">
                        <span>16:00</span>
                        <span>20:00</span>
                        <span>23:00</span>
                    </div>
                </div>

            </div>
        </div>
    </main>

    <script>
        var map = new trackasiagl.Map({
            container: 'map',
            style: 'https://maps.track-asia.com/styles/v2/streets.json?key=bca01773651908dcc9bc6320f7c16973ce',
            center: [106.703614, 10.761089],
            zoom: 17
        });

        map.addControl(new trackasiagl.NavigationControl(), 'top-left');

        // Mảng lưu trữ tất cả các marker để xử lý search
        const allMarkers = [];

        map.on('load', () => {
            const pois = <?php echo json_encode($pois); ?>;

            pois.forEach(poi => {
                // Ensure key access works across lowercase vs original database arrays
                const poiId = poi.Id || poi.id;
                const lat = parseFloat(poi.Latitude || poi.latitude || 0);
                const lng = parseFloat(poi.Longitude || poi.longitude || 0);
                const radius = parseFloat(poi.triggerRadiusMeters || poi.triggerradiusmeters || poi.triggerRadiusmeters || 20);
                const isActive = parseInt(poi.IsActive ?? poi.isactive ?? poi.isActive ?? 0) === 1;
                const name = poi.Name || poi.name || "Không tên";

                if (isNaN(lat) || isNaN(lng) || (lat === 0 && lng === 0)) return;

                const center = [lng, lat];

                // Radius Circle via Turf.js
                if (radius > 0) {
                    const circleCoords = turf.circle(center, radius, {steps: 64, units: 'meters'});
                    
                    const sourceId = 'radius-source-' + poiId;
                    map.addSource(sourceId, {
                        type: 'geojson',
                        data: circleCoords
                    });

                    // Circle Fill layer
                    map.addLayer({
                        id: 'radius-fill-' + poiId,
                        type: 'fill',
                        source: sourceId,
                        layout: {
                            'visibility': isActive ? 'visible' : 'none'
                        },
                        paint: {
                            'fill-color': isActive ? '#FF4D15' : '#6B7280',
                            'fill-opacity': isActive ? 0.15 : 0.1,
                        }
                    });

                    // Circle Outline layer
                    map.addLayer({
                        id: 'radius-outline-' + poiId,
                        type: 'line',
                        source: sourceId,
                        layout: {
                            'visibility': isActive ? 'visible' : 'none'
                        },
                        paint: {
                            'line-color': isActive ? '#FF4D15' : '#6B7280',
                            'line-width': 1.5,
                            'line-opacity': 0.8
                        }
                    });
                }

                // Custom element for the marker pin
                const el = document.createElement('div');
                el.className = 'pointer-events-auto cursor-pointer';
                if (!isActive) el.style.display = 'none';
                
                const inner = document.createElement('div');
                inner.className = 'w-8 h-8 rounded-full flex items-center justify-center text-white shadow-[0_2px_8px_rgba(0,0,0,0.3)] border-[2px] border-white transition-transform hover:scale-110';
                inner.style.backgroundColor = isActive ? '#FF4D15' : '#6B7280';
                inner.innerHTML = '<i class="fas fa-map-marker-alt text-[14px]"></i>';
                el.appendChild(inner);

                // Xử lý lấy hình ảnh
                let imageUrl = poi.avatarUrl || poi.avatarurl || '';
                
                if (!imageUrl) {
                    const rawImageUrls = poi.ImageUrls || poi.imageurls || '[]';
                    try {
                        const parsed = JSON.parse(rawImageUrls);
                        if (Array.isArray(parsed) && parsed.length > 0) {
                            imageUrl = parsed[0];
                        }
                    } catch (e) {
                        if (typeof rawImageUrls === 'string' && rawImageUrls.length > 5) imageUrl = rawImageUrls;
                    }
                }

                // Tạo HTML cho popup
                const imgHtml = imageUrl ? `<img src="${imageUrl}" alt="${name}" class="w-full h-32 object-cover rounded-lg mb-2 shadow-sm">` : '';

                const qrDataHover = encodeURIComponent(`vinhkhanh://poi?id=${poiId}&action=play`);
                const qrUrlHover = `https://api.qrserver.com/v1/create-qr-code/?size=100x100&data=${qrDataHover}`;

                const popupContent = `
                    <div class="px-1 py-1 w-56">
                        ${imgHtml}
                        <div class="flex gap-2 items-start mb-2">
                            <div class="flex-1">
                                <h3 class="font-bold text-gray-800 text-[15px] leading-tight">${name}</h3>
                            </div>
                            <img src="${qrUrlHover}" alt="QR Code" class="w-10 h-10 object-contain shadow-sm border border-gray-200 rounded p-0.5 bg-white">
                        </div>
                        <div class="text-[12px] text-gray-600 space-y-1.5 mb-2 bg-gray-50 p-2 rounded-lg border border-gray-100">
                            <p class="flex items-center gap-2"><i class="fas fa-map-marker text-red-500 w-3 text-center"></i> Vĩ độ: <b>${lat.toFixed(5)}</b></p>
                            <p class="flex items-center gap-2"><i class="fas fa-map-marker text-blue-500 w-3 text-center"></i> Kinh độ: <b>${lng.toFixed(5)}</b></p>
                            <p class="flex items-center gap-2"><i class="fas fa-arrows-alt-h text-primary w-3 text-center"></i> Bán kính: <b>${radius}m</b></p>
                        </div>
                        <p class="text-[12px] text-gray-500 italic mb-2"><i class="fas fa-click text-blue-500"></i> Click để xem chi tiết</p>
                    </div>
                `;

                const popup = new trackasiagl.Popup({ 
                    offset: 20, 
                    closeButton: false, // Ẩn nút đóng vì dùng hover
                    closeOnClick: false 
                }).setHTML(popupContent);

                const marker = new trackasiagl.Marker({
                    element: el
                })
                .setLngLat(center)
                .addTo(map);

                // Hiển thị popup khi hover
                el.addEventListener('mouseenter', () => {
                    popup.setLngLat(center).addTo(map);
                });

                // Đóng popup khi rời chuột
                el.addEventListener('mouseleave', () => {
                    popup.remove();
                });

                // Click để mở modal
                el.addEventListener('click', () => {
                    openPoiModal(poi);
                });

                // Lưu lại thông tin marker để tìm kiếm
                allMarkers.push({
                    id: poiId,
                    name: name,
                    address: poi.Address || poi.address || '',
                    marker: marker,
                    popup: popup,
                    element: el,
                    center: center,
                    isActive: isActive,
                    radius: radius
                });
            });

            // Toggle Inactive POI
            const toggleInactivePOI = document.getElementById('toggleInactivePOI');
            if (toggleInactivePOI) {
                toggleInactivePOI.addEventListener('change', function(e) {
                    const isShow = e.target.checked;
                    allMarkers.forEach(m => {
                        if (!m.isActive) {
                            m.element.style.display = isShow ? 'block' : 'none';
                            
                            if (m.radius > 0) {
                                const visibility = isShow ? 'visible' : 'none';
                                if (map.getLayer('radius-fill-' + m.id)) {
                                    map.setLayoutProperty('radius-fill-' + m.id, 'visibility', visibility);
                                }
                                if (map.getLayer('radius-outline-' + m.id)) {
                                    map.setLayoutProperty('radius-outline-' + m.id, 'visibility', visibility);
                                }
                            }
                        }
                    });
                });
            }

            // Lấy tọa độ (kinh độ, vĩ độ) khi click chuột phải lên bản đồ
            map.on('contextmenu', (e) => {
                const lat = e.lngLat.lat.toFixed(6);
                const lng = e.lngLat.lng.toFixed(6);
                const coordsText = `${lat}, ${lng}`;
                
                // Cố gắng copy thẳng vào clipboard
                navigator.clipboard.writeText(coordsText).catch(err => console.log('Không thể auto-copy', err));

                const popupContent = `
                    <div class="px-1 py-1 text-center w-36">
                        <p class="text-[13px] font-bold text-gray-800 mb-2 leading-tight">
                            <i class="fas fa-check-circle text-green-500 mr-1"></i>Đã copy tọa độ
                        </p>
                        <div class="bg-gray-50 p-2 rounded-lg text-[12px] font-mono text-gray-700 border border-gray-200 select-all">
                            ${lat}<br>${lng}
                        </div>
                    </div>
                `;

                // Tạo popup hiển thị tại vị trí click chuột phải
                const clickPopup = new trackasiagl.Popup({ offset: 0, closeButton: false, closeOnClick: true })
                    .setLngLat(e.lngLat)
                    .setHTML(popupContent)
                    .addTo(map);
                
                // Tự động đóng popup sau 2.5 giây cho đỡ vướng
                setTimeout(() => {
                    clickPopup.remove();
                }, 2500);
            });

            // ====== KHỞI TẠO TÍNH NĂNG HEATMAP ======
            map.addSource('heatmap-source', {
                type: 'geojson',
                data: { type: 'FeatureCollection', features: [] }
            });

            map.addLayer({
                id: 'heatmap-layer',
                type: 'heatmap',
                source: 'heatmap-source',
                maxzoom: 24,
                paint: {
                    // Cường độ tăng dần theo mật độ gom nhóm
                    'heatmap-weight': 1,
                    // Linear scale điều chỉnh màu sắc từ không có đến nhạt đến đậm
                    'heatmap-intensity': [
                        'interpolate', ['linear'], ['zoom'],
                        13, 0.5,
                        18, 2
                    ],
                    'heatmap-color': [
                        'interpolate', ['linear'], ['heatmap-density'],
                        0, 'rgba(33,102,172,0)',
                        0.2, 'rgba(255,189,102,0.6)',
                        0.5, 'rgba(244,169,80,0.8)',
                        0.8, 'rgba(239,68,68,0.9)',
                        1, 'rgba(185,28,28,1)'
                    ],
                    'heatmap-radius': [
                        'interpolate', ['linear'], ['zoom'],
                        0, 5,
                        15, 25,
                        20, 55
                    ],
                    'heatmap-opacity': 0.8
                }
            });

            // Lắng nghe sự kiện bật tắt Heatmap
            const toggleHeatmap = document.getElementById('toggleHeatmap');
            if (toggleHeatmap) {
                toggleHeatmap.addEventListener('change', function(e) {
                    const isVisible = e.target.checked;
                    map.setLayoutProperty('heatmap-layer', 'visibility', isVisible ? 'visible' : 'none');
                });
            }

            // Variables lưu dữ liệu Firebase để khỏi fetch nhiều lần 
            let _visitLogs = [];
            let _audioLogs = [];
            let _presenceLogs = [];
            let isLogFetched = false;

            // Toggle hiển thị thanh Time Slider
            const toggleTimeFilter = document.getElementById('toggleTimeFilter');
            const timeSliderContainer = document.getElementById('timeSliderContainer');
            const timeSlider = document.getElementById('timeSlider');
            const timeSliderVal = document.getElementById('timeSliderVal');

            if (toggleTimeFilter) {
                toggleTimeFilter.addEventListener('change', function(e) {
                    const isTimeFilterOn = e.target.checked;
                    timeSliderContainer.style.display = isTimeFilterOn ? 'flex' : 'none';
                    if (isLogFetched) {
                        applyHeatmapFilter(isTimeFilterOn ? parseInt(timeSlider.value) : -1);
                    }
                });
            }

            // Xử lý slider thay đổi
            timeSlider.addEventListener('input', function() {
                const hour = parseInt(this.value);
                timeSliderVal.textContent = `${hour}:00 - ${hour}:59`;
                if(isLogFetched && toggleTimeFilter.checked) applyHeatmapFilter(hour);
            });

            // Hàm áp dụng logic lọc theo khung giờ và tính HotScore
            function applyHeatmapFilter(targetHour) {
                const features = [];
                const poiScores = {}; // poiId -> { sessions: Set, completions: [], presences: 0 }

                const getHour = (isoString) => {
                    if (!isoString) return -1;
                    const d = new Date(isoString);
                    return d.getHours();
                };

                // Helper để add feature điểm nhiệt độc lập (phân tán xung quanh quán)
                const addHeatmapFeature = (poiId, logLat, logLng) => {
                    let lat = parseFloat(logLat || 0);
                    let lng = parseFloat(logLng || 0);

                    // Fallback to POI center if coords missing
                    if ((isNaN(lat) || isNaN(lng) || lat === 0 || lng === 0) && poiId) {
                        const marker = allMarkers.find(m => m.id === poiId);
                        if (marker) {
                            lat = marker.center[1] + (Math.random() - 0.5) * 0.00015;
                            lng = marker.center[0] + (Math.random() - 0.5) * 0.00015;
                        }
                    }
                    if (!isNaN(lat) && !isNaN(lng) && lat !== 0 && lng !== 0) {
                        features.push({
                            type: 'Feature',
                            geometry: { type: 'Point', coordinates: [lng, lat] }
                        });
                    }
                };

                // Tính điểm visit
                _visitLogs.forEach(v => {
                    if (targetHour !== -1 && getHour(v.visitTime) !== targetHour) return;
                    addHeatmapFeature(v.poiId, v.userLat || v.latitude || v.lat, v.userLng || v.longitude || v.lng);
                    
                    const pid = v.poiId;
                    if (!pid) return;
                    if (!poiScores[pid]) poiScores[pid] = { sessions: new Set(), completions: [], presences: 0 };
                    if (v.sessionId) poiScores[pid].sessions.add(v.sessionId);
                });

                // Tính điểm audio
                _audioLogs.forEach(a => {
                    if (targetHour !== -1 && getHour(a.playTime) !== targetHour) return;
                    addHeatmapFeature(a.poiId, a.userLat || a.latitude || a.lat, a.userLng || a.longitude || a.lng);

                    const pid = a.poiId;
                    if (!pid) return;
                    if (!poiScores[pid]) poiScores[pid] = { sessions: new Set(), completions: [], presences: 0 };
                    poiScores[pid].completions.push(a.completionRate || 0);
                });

                // Tính điểm User Presence realtime (updatedAt)
                _presenceLogs.forEach(p => {
                    if (targetHour !== -1 && getHour(p.updatedAt) !== targetHour) return;
                    addHeatmapFeature(p.poiId, p.latitude || p.lat, p.longitude || p.lng);

                    const pid = p.poiId;
                    if (!pid) return;
                    if (!poiScores[pid]) poiScores[pid] = { sessions: new Set(), completions: [], presences: 0 };
                    poiScores[pid].presences++;
                });

                allMarkers.forEach(m => {
                    m.element.classList.remove('poi-fire');
                    const badge = m.element.querySelector('.poi-hot-badge');
                    if (badge) badge.remove();

                    const pid = m.id;
                    const stats = poiScores[pid];
                    if (!stats) return;

                    const averageHistory = stats.sessions.size;
                    const avgCompletion = stats.completions.length > 0 
                                            ? stats.completions.reduce((a, b) => a + b, 0) / stats.completions.length 
                                            : 0;
                    const currentStrangers = stats.presences;

                    // Công thức HotScore từ App C# 
                    let score = (averageHistory * 10) + (avgCompletion * 0.3) + (currentStrangers * 20);
                    score = Math.min(100, Math.max(0, score)); // Clamp 0-100

                    // Chỉ kích hoạt render CSS HOT khi bộ lọc thời gian đang bật
                    if (targetHour !== -1 && score > 30) {
                        m.element.classList.add('poi-fire');
                        const b = document.createElement('div');
                        b.className = 'poi-hot-badge bg-red-500 text-white text-[10px] px-1.5 py-0.5 rounded-md absolute -top-3 -right-3 font-bold shadow animate-bounce';
                        b.innerHTML = '🔥 HOT';
                        m.element.appendChild(b);
                    }
                });

                // Cập nhật lên Source - Dùng features mới gom đủ từng điểm nhỏ của log!
                map.getSource('heatmap-source').setData({
                    type: 'FeatureCollection',
                    features: features
                });
            }

            // Gọi Firebase lấy dữ liệu cho Heatmap
            async function loadHeatmapData() {
                try {
                    const audioUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/AudioPlayLog.json";
                    const visitUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/VisitLog.json";
                    const presenceUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/UserPresence.json";

                    const [audioRes, visitRes, presenceRes] = await Promise.all([
                        fetch(audioUrl), fetch(visitUrl), fetch(presenceUrl)
                    ]);
                    
                    const audioData = await audioRes.json() || {};
                    const visitData = await visitRes.json() || {};
                    const presenceData = await presenceRes.json() || {};
                    
                    _audioLogs = Object.values(audioData).filter(x => x);
                    _visitLogs = Object.values(visitData).filter(x => x);
                    _presenceLogs = Object.values(presenceData).filter(x => x);
                    isLogFetched = true;

                    // Lọc lần đầu tiên (hiển thị All Time vì toggle tắt)
                    applyHeatmapFilter(-1);

                } catch (e) {
                    console.error('Lỗi khi tải dữ liệu Heatmap:', e);
                }
            }

            // Tiến hành gọi data
            loadHeatmapData();

        });

        // Search Implementation
        const searchInput = document.getElementById('searchInput');
        const searchResults = document.getElementById('searchResults');
        let activePopup = null; // Store reference to currently opened popup

        // Function normalize string (remove accents, to lower case)
        function removeAccents(str) {
            if (!str) return '';
            return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
        }

        searchInput.addEventListener('input', function() {
            const val = this.value.trim();
            if (val.length === 0) {
                searchResults.innerHTML = '';
                searchResults.classList.add('hidden');
                return;
            }

            const normalizedSearch = removeAccents(val);
            const toggleInactivePOI = document.getElementById('toggleInactivePOI');
            const showInactive = toggleInactivePOI ? toggleInactivePOI.checked : false;

            const matches = allMarkers.filter(m => {
                if (!showInactive && !m.isActive) return false;
                return removeAccents(m.name).includes(normalizedSearch) || removeAccents(m.address).includes(normalizedSearch);
            });

            if (matches.length > 0) {
                searchResults.innerHTML = matches.map(m => `
                    <div class="px-4 py-3 border-b border-gray-100 hover:bg-brand-50 cursor-pointer transition-colors" onclick="focusOnPoi('${m.id}')">
                        <div class="font-bold text-gray-800 text-sm">${m.name}</div>
                        <div class="text-xs text-gray-500 truncate mt-1"><i class="fas fa-map-marker-alt mr-1 text-gray-400"></i>${m.address || 'Không có địa chỉ'}</div>
                    </div>
                `).join('');
                searchResults.classList.remove('hidden');
            } else {
                searchResults.innerHTML = '<div class="px-4 py-3 text-sm text-gray-500 text-center">Không tìm thấy quán nào...</div>';
                searchResults.classList.remove('hidden');
            }
        });

        // Hide results on outside click
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
                searchResults.classList.add('hidden');
            }
        });

        // Logic Modal Chi tiết POI
        const poiDetailModal = document.getElementById('poiDetailModal');
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

        // Focus function
        window.focusOnPoi = function(id) {
            const target = allMarkers.find(m => m.id == id);
            if (target) {
                // Focus Map
                map.flyTo({
                    center: target.center,
                    zoom: 19,
                    duration: 1500,
                    essential: true 
                });

                // Add bounce animation to marker
                target.element.querySelector('div').classList.add('animate-bounce');
                
                // Đóng popup cũ nếu có
                if(activePopup) { activePopup.remove(); }

                // Hiển thị popup thay vì mở thẳng modal
                setTimeout(() => {
                    target.popup.setLngLat(target.center).addTo(map);
                    activePopup = target.popup;
                    
                    // Stop bouncing after 3s
                    setTimeout(() => {
                        target.element.querySelector('div').classList.remove('animate-bounce');
                    }, 2000);
                }, 1000); 
                
                // Set input to result name and close dropdown
                searchInput.value = target.name;
                searchResults.classList.add('hidden');
            }
        };

        const ELEVEN_LABS_API_KEY = "";
        
        function base64ToArrayBuffer(base64) {
            const binaryString = atob(base64);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes;
        }

        function createWavFile(pcmData, sampleRate = 24000) {
            const buffer = new ArrayBuffer(44 + pcmData.length);
            const view = new DataView(buffer);
            function writeString(view, offset, str) {
                for (let i = 0; i < str.length; i++) {
                    view.setUint8(offset + i, str.charCodeAt(i));
                }
            }
            writeString(view, 0, 'RIFF');
            view.setUint32(4, 36 + pcmData.length, true);
            writeString(view, 8, 'WAVE');
            writeString(view, 12, 'fmt ');
            view.setUint32(16, 16, true);
            view.setUint16(20, 1, true);
            view.setUint16(22, 1, true);
            view.setUint32(24, sampleRate, true);
            view.setUint32(28, sampleRate * 2, true);
            view.setUint16(32, 2, true);
            view.setUint16(34, 16, true);
            writeString(view, 36, 'data');
            view.setUint32(40, pcmData.length, true);
            const wavBytes = new Uint8Array(buffer);
            wavBytes.set(pcmData, 44);
            return new Blob([wavBytes], { type: "audio/wav" });
        }

        let currentAudioMap = null;

        async function playTTS(lang, text) {
            if (!text) {
                alert("Không có nội dung để đọc.");
                return;
            }
            stopTTS();
            try {
                const toast = document.createElement('div');
                toast.id = 'tts_toast_loading';
                toast.className = 'fixed bottom-4 right-4 bg-blue-600 text-white px-4 py-2 rounded-lg shadow-lg z-50 text-sm flex items-center gap-2';
                toast.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gọi API TTS...';
                document.body.appendChild(toast);

                let audioUrl;
                if (lang === 'vi') {
                    const response = await fetch(
                        "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key=",
                        {
                            method: "POST",
                            headers: { "Content-Type": "application/json" },
                            body: JSON.stringify({
                                contents: [{ parts: [{ text: text }] }],
                                generationConfig: { responseModalities: ["AUDIO"] }
                            })
                        }
                    );
                    const data = await response.json();
                    if (!data.candidates) {
                        document.getElementById('tts_toast_loading')?.remove();
                        alert("API lỗi: " + (data.error?.message || "Unknown"));
                        return;
                    }
                    const base64 = data.candidates[0].content.parts[0].inlineData.data;
                    const pcmData = base64ToArrayBuffer(base64);
                    const wavBlob = createWavFile(pcmData);
                    audioUrl = URL.createObjectURL(wavBlob);
                } else {
                    const voiceId = "EXAVITQu4vr4xnSDxMaL";
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
                        document.getElementById('tts_toast_loading')?.remove();
                        alert("Lỗi kết nối ElevenLabs API: " + errText);
                        return;
                    }
                    const audioBlob = await response.blob();
                    audioUrl = URL.createObjectURL(audioBlob);
                }
                currentAudioMap = new Audio(audioUrl);
                currentAudioMap.play();
                document.getElementById('tts_toast_loading').innerHTML = '<i class="fas fa-volume-up"></i> Đang đọc...';
                currentAudioMap.onended = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    URL.revokeObjectURL(audioUrl);
                };
                currentAudioMap.onerror = () => {
                    document.getElementById('tts_toast_loading')?.remove();
                    alert("Không thể phát file âm thanh.");
                };
            } catch (err) {
                console.error('Fetch Error:', err);
                document.getElementById('tts_toast_loading')?.remove();
                alert("Lỗi gọi API TTS.");
            }
        }

        function stopTTS() {
            if (currentAudioMap) {
                currentAudioMap.pause();
                currentAudioMap.currentTime = 0;
            }
            document.getElementById('tts_toast_loading')?.remove();
            if ('speechSynthesis' in window) {
                window.speechSynthesis.cancel();
            }
        }

    </script>
</body>
</html>