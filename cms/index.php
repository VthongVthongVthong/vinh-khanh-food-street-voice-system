<?php
session_start();
if (!isset($_SESSION['user_id']) || strtoupper($_SESSION['role']) !== 'ADMIN') {
    header("Location: login.php");
    exit;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vĩnh Khánh CMS - Tổng quan</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
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
                <a href="index.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
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
                    <input type="text" placeholder="Tìm kiếm nhanh..." class="w-full pl-10 pr-4 py-2 bg-gray-50 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all text-sm">
                </div>
            </div>
            
            <div class="flex items-center gap-4">
                <button class="relative p-2 text-gray-400 hover:text-gray-600 transition-colors">
                    <i class="far fa-bell text-xl"></i>
                    <span class="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full border-2 border-white"></span>
                </button>
                <div class="h-8 w-px bg-gray-200 hidden sm:block"></div>
                <div class="flex items-center gap-3 cursor-pointer">
                    <div class="w-10 h-10 rounded-full bg-brand-100 text-brand-600 flex items-center justify-center font-bold">
                        A
                    </div>
                    <div class="hidden sm:block">
                        <p class="text-sm font-semibold text-gray-700">Admin</p>
                        <p class="text-xs text-gray-500">Quản trị viên</p>
                    </div>
                </div>
            </div>
        </header>

        <!-- Dashboard Content -->
        <div class="p-6 md:p-8 w-full max-w-7xl mx-auto space-y-6">
            
            <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <h2 class="text-2xl font-bold text-gray-800">Tổng quan hệ thống</h2>
                <span id="lastUpdateText" class="text-sm text-gray-500">Cập nhật lần cuối: <?php echo date('H:i:s'); ?></span>
            </div>

            <?php
            // Kết nối CSDL tạm (sau này sẽ query thực tế)
            require_once 'db.php';
            $db = new SQLiteDB();
            $pdo = $db->getPDO();
            
            // Xử lý ngày tuần được chọn để lọc
            $selectedDateStr = isset($_GET['date']) ? $_GET['date'] : date('Y-m-d');
            $selectedTime = strtotime($selectedDateStr);
            $dayOfWeek = date('N', $selectedTime); // 1 = Monday, 7 = Sunday
            $mondayTime = strtotime('-' . ($dayOfWeek - 1) . ' days', $selectedTime);
            $mondayDate = date('Y-m-d', $mondayTime);
            $prevWeek = date('Y-m-d', strtotime('-7 days', $mondayTime));
            $nextWeek = date('Y-m-d', strtotime('+7 days', $mondayTime));
            
            // Lấy danh sách các ngày trong tuần đang xét
            $weekDays = [];
            $interactionData = [0, 0, 0, 0, 0, 0, 0];
            $chartLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
            for ($i = 0; $i < 7; $i++) {
                $currentDay = date('Y-m-d', strtotime("+$i days", $mondayTime));
                $weekDays[] = $currentDay;
                $chartLabels[$i] .= " (" . date('d/m', strtotime("+$i days", $mondayTime)) . ")";
            }

            // Đếm tổng user
            $userCount = 0;
            try {
                $stmt = $pdo->query("SELECT COUNT(id) FROM User");
                $userCount = $stmt->fetchColumn();
            } catch (Exception $e) {}

            // Đếm tổng guest từ Firebase
            $guestCount = 0;
            try {
                $firebaseGuestUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/GuestSession.json?shallow=true";
                $chg = curl_init();
                curl_setopt($chg, CURLOPT_URL, $firebaseGuestUrl);
                curl_setopt($chg, CURLOPT_RETURNTRANSFER, 1);
                curl_setopt($chg, CURLOPT_SSL_VERIFYPEER, false);
                $respGuest = curl_exec($chg);
                curl_close($chg);
                
                if ($respGuest && $respGuest !== 'null') {
                    $guessDataObj = json_decode($respGuest, true);
                    if (is_array($guessDataObj)) {
                        $guestCount = count($guessDataObj);
                    }
                }
            } catch (Exception $e) {}
            $totalUserCount = $userCount + $guestCount;

            // Đếm tổng lượt nghe và Top POI từ Firebase
            $audioPlayCount = 0;
            $topPlacesLabels = [];
            $topPlacesData = [];

            try {
                // Ta cần lấy toàn bộ dữ liệu để đếm tần suất POI và gom nhóm tuần
                $firebaseUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/AudioPlayLog.json";
                $ch = curl_init();
                curl_setopt($ch, CURLOPT_URL, $firebaseUrl);
                curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
                curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false);
                $response = curl_exec($ch);
                curl_close($ch);
                
                if ($response && $response !== 'null') {
                    $data = json_decode($response, true);
                    if (is_array($data)) {
                        $audioPlayCount = count($data);
                        
                        // Xử lý đếm lượt phát POI tổng
                        $poiPlayCounts = [];
                        foreach ($data as $key => $log) {
                            if (is_array($log) && !empty($log['poiId'])) {
                                $poiId = (int)$log['poiId'];
                                $poiPlayCounts[$poiId] = ($poiPlayCounts[$poiId] ?? 0) + 1;
                            }
                            
                            // Phân loại data theo tuần
                            $timeStr = isset($log['playtime']) ? $log['playtime'] : (isset($log['timestamp']) ? $log['timestamp'] : null);
                            if ($timeStr) {
                                // Xử lý timestamp millis hoặc chuẩn ngày tháng
                                $time = is_numeric($timeStr) ? ($timeStr > 10000000000 ? $timeStr/1000 : $timeStr) : strtotime($timeStr);
                                $logDate = date('Y-m-d', (int)$time);
                                $index = array_search($logDate, $weekDays);
                                if ($index !== false) {
                                    $interactionData[$index]++;
                                }
                            }
                        }
                        
                        // Sắp xếp giảm dần và lấy top 4
                        arsort($poiPlayCounts);
                        $topPoiIds = array_slice($poiPlayCounts, 0, 4, true);
                        
                        if (!empty($topPoiIds)) {
                            // Lấy tên POI từ database MySQL
                            $idList = implode(',', array_keys($topPoiIds));
                            $stmt = $pdo->query("SELECT id, name FROM POI WHERE id IN ($idList)");
                            $poiNames = [];
                            while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
                                $poiNames[$row['id']] = $row['name'];
                            }
                            
                            foreach ($topPoiIds as $pId => $count) {
                                // Tách chuỗi tên dài thành mảng nhỏ để tooltip biểu đồ đẹp hơn nếu cần
                                // Ở đây đơn giản lấy tên hoặc ID nếu không tìm thấy
                                $name = isset($poiNames[$pId]) ? $poiNames[$pId] : "POI $pId";
                                $topPlacesLabels[] = explode(" ", $name, 2); // Tách 2 dòng cho khớp style chart
                                $topPlacesData[] = $count;
                            }
                        }
                    }
                }
            } catch (Exception $e) {}

            // Thêm cả lượt tương tác từ VisitLog
            try {
                $firebaseVisitUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/VisitLog.json";
                $chv = curl_init();
                curl_setopt($chv, CURLOPT_URL, $firebaseVisitUrl);
                curl_setopt($chv, CURLOPT_RETURNTRANSFER, 1);
                curl_setopt($chv, CURLOPT_SSL_VERIFYPEER, false);
                $responseVisit = curl_exec($chv);
                curl_close($chv);
                
                if ($responseVisit && $responseVisit !== 'null') {
                    $visitData = json_decode($responseVisit, true);
                    if (is_array($visitData)) {
                        foreach ($visitData as $key => $log) {
                            $timeStr = isset($log['playtime']) ? $log['playtime'] : (isset($log['timestamp']) ? $log['timestamp'] : null);
                            // Dùng fall-back nếu trường tên là visitTime hoặc time
                            if (!$timeStr) $timeStr = isset($log['visitTime']) ? $log['visitTime'] : (isset($log['time']) ? $log['time'] : null);
                            
                            if ($timeStr) {
                                $time = is_numeric($timeStr) ? ($timeStr > 10000000000 ? $timeStr/1000 : $timeStr) : strtotime($timeStr);
                                $logDate = date('Y-m-d', (int)$time);
                                $index = array_search($logDate, $weekDays);
                                if ($index !== false) {
                                    $interactionData[$index]++; // Cộng dồn số lượt vào interaction
                                }
                            }
                        }
                    }
                }
            } catch (Exception $e) {}
            
            // Query thử số lượng POI
            $poiCount = 0;
            try {
                $stmt = $pdo->query("SELECT COUNT(id) FROM POI");
                $poiCount = $stmt->fetchColumn();
            } catch (Exception $e) {}

            // Đếm tổng Tour
            $tourCount = 0;
            try {
                $stmt = $pdo->query("SELECT COUNT(id) FROM Tour");
                $tourCount = $stmt->fetchColumn();
            } catch (Exception $e) {}

            // Nếu là request ajax thì trả về JSON rồi kết thúc
            if (isset($_GET['ajax']) && $_GET['ajax'] == '1') {
                header('Content-Type: application/json');
                echo json_encode([
                    'totalUserCount' => $totalUserCount,
                    'userCount' => $userCount,
                    'guestCount' => $guestCount,
                    'audioPlayCount' => $audioPlayCount,
                    'poiCount' => $poiCount,
                    'tourCount' => $tourCount,
                    'chartLabels' => $chartLabels,
                    'interactionData' => $interactionData,
                    'topPlacesLabels' => $topPlacesLabels,
                    'topPlacesData' => $topPlacesData
                ]);
                exit;
            }
            ?>

            <!-- Stats Grid -->
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                <!-- Stat Card 1 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tổng lượt người dùng</p>
                            <h3 id="statTotalUsers" class="text-2xl font-bold text-gray-800"><?php echo number_format($totalUserCount, 0, ',', '.'); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-users"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center justify-between text-sm">
                        <span id="statGuestCount" class="text-gray-500 flex items-center gap-1" title="Khách vãng lai">
                           <i class="far fa-user text-gray-400"></i> <?php echo number_format($guestCount, 0, ',', '.'); ?>
                        </span>
                        <span id="statMemberCount" class="text-brand-500 flex items-center gap-1" title="Thành viên">
                           <i class="fas fa-user-check text-brand-400"></i> <?php echo number_format($userCount, 0, ',', '.'); ?>
                        </span>
                    </div>
                </div>

                <!-- Stat Card 2 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tổng lượt phát Audio</p>
                            <h3 id="statAudioPlays" class="text-2xl font-bold text-gray-800"><?php echo number_format($audioPlayCount, 0, ',', '.'); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-headphones"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-green-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-trend-up"></i> +0%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>

                <!-- Stat Card 3 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Điểm POI Active</p>
                            <h3 id="statPoiCount" class="text-2xl font-bold text-gray-800"><?php echo number_format($poiCount, 0, ',', '.'); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-map-marker-alt"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-green-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-right"></i> 0%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>

                <!-- Stat Card 4 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tour đang hoạt động</p>
                            <h3 id="statTourCount" class="text-2xl font-bold text-gray-800"><?php echo number_format($tourCount, 0, ',', '.'); ?></h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-red-50 text-red-500 flex items-center justify-center">
                            <i class="fas fa-route"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-red-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-trend-down"></i> -0%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>
            </div>

            <!-- Charts Section -->
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
                <!-- Line Chart -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between mb-4 gap-2">
                        <h3 class="text-lg font-bold text-gray-800">Lượt tương tác theo tuần</h3>
                        <div class="flex items-center gap-2 border border-gray-200 rounded-lg p-1">
                            <a href="?date=<?php echo $prevWeek; ?>" class="px-2 py-1 text-gray-400 hover:text-gray-700 hover:bg-gray-50 rounded transition-colors" title="Tuần trước">
                                <i class="fas fa-chevron-left"></i>
                            </a>
                            <input type="date" class="text-sm px-2 py-1 text-gray-600 focus:outline-none bg-transparent cursor-pointer font-medium" 
                                value="<?php echo $selectedDateStr; ?>" 
                                onchange="window.location.href='?date='+this.value">
                            <a href="?date=<?php echo $nextWeek; ?>" class="px-2 py-1 text-gray-400 hover:text-gray-700 hover:bg-gray-50 rounded transition-colors" title="Tuần sau">
                                <i class="fas fa-chevron-right"></i>
                            </a>
                        </div>
                    </div>
                    <div class="h-64 sm:h-80 w-full relative">
                        <canvas id="interactionChart"></canvas>
                    </div>
                </div>

                <!-- Bar Chart -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                    <h3 class="text-lg font-bold text-gray-800 mb-4">Top địa điểm được nghe nhiều nhất</h3>
                    <div class="h-64 sm:h-80 w-full relative">
                        <canvas id="topPlacesChart"></canvas>
                    </div>
                </div>
            </div>

        </div>
    </main>

    <!-- Chart Configuration -->
    <script>
        // Lấy dữ liệu chart từ PHP
        const chartLabelsDate = <?php echo json_encode($chartLabels); ?>;
        const chartInteractionData = <?php echo json_encode($interactionData); ?>;

        // Cấu hình chung cho Chart.js
        Chart.defaults.font.family = "'Inter', 'Segoe UI', sans-serif";
        Chart.defaults.color = '#9CA3AF';
        
        // Line Chart: Lượt tương tác
        const ctxInteraction = document.getElementById('interactionChart').getContext('2d');
        const maxInteractionValue = Math.max(...chartInteractionData) || 10;
        const interactionStepSize = Math.ceil(maxInteractionValue / 5) || 2;
        
        const interactionChartIns = new Chart(ctxInteraction, {
            type: 'line',
            data: {
                labels: chartLabelsDate,
                datasets: [{
                    label: 'Lượt tương tác',
                    data: chartInteractionData,
                    borderColor: '#FF4D15',
                    backgroundColor: 'rgba(255, 77, 21, 0.1)',
                    borderWidth: 2,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: '#FF4D15',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    tension: 0.4 // Làm cong đường
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1F2937',
                        padding: 12,
                        cornerRadius: 8,
                        displayColors: false,
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: maxInteractionValue + interactionStepSize, // Tự động co giãn theo giá trị cao nhất
                        ticks: { stepSize: interactionStepSize },
                        grid: { borderDash: [4, 4], color: '#F3F4F6', drawBorder: false }
                    },
                    x: {
                        grid: { display: false, drawBorder: false }
                    }
                }
            }
        });

        // Bar Chart: Top địa điểm (Ngang)
        const topPlacesLabelsRaw = <?php echo json_encode($topPlacesLabels); ?>;
        const topPlacesDataRaw = <?php echo json_encode($topPlacesData); ?>;
        
        let labelsToUse = topPlacesLabelsRaw.length > 0 ? topPlacesLabelsRaw : [['Chưa có', 'dữ liệu']];
        let dataToUse = topPlacesDataRaw.length > 0 ? topPlacesDataRaw : [0];
        
        // Tính max động để set biểu đồ
        const maxDataValue = Math.max(...dataToUse) || 10;
        const stepSizeToUse = Math.ceil(maxDataValue / 4);

        const ctxTopPlaces = document.getElementById('topPlacesChart').getContext('2d');
        const topPlacesChartIns = new Chart(ctxTopPlaces, {
            type: 'bar',
            data: {
                labels: labelsToUse,
                datasets: [{
                    data: dataToUse,
                    backgroundColor: '#FF4D15',
                    borderRadius: 20,
                    barThickness: 24,
                    borderSkipped: false
                }]
            },
            options: {
                indexAxis: 'y', // Đảo thành biểu đồ ngang
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1F2937',
                        padding: 12,
                        cornerRadius: 8,
                        displayColors: false,
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        max: maxDataValue + stepSizeToUse, // Tự động co giãn theo giá trị data cao nhất
                        ticks: { stepSize: stepSizeToUse },
                        grid: { display: false, drawBorder: false }
                    },
                    y: {
                        grid: { display: false, drawBorder: false },
                        ticks: {
                            font: { size: 11 },
                            color: '#4B5563'
                        }
                    }
                }
            }
        });

        // Tự động làm mới dữ liệu
        setInterval(async () => {
            try {
                const urlParams = new URLSearchParams(window.location.search);
                urlParams.set('ajax', '1');
                
                const res = await fetch('?' + urlParams.toString());
                if (!res.ok) return;
                const data = await res.json();
                
                // Update UI text
                document.getElementById('statTotalUsers').innerText = data.totalUserCount.toLocaleString('vi-VN');
                document.getElementById('statGuestCount').innerHTML = `<i class="far fa-user text-gray-400"></i> ${data.guestCount.toLocaleString('vi-VN')}`;
                document.getElementById('statMemberCount').innerHTML = `<i class="fas fa-user-check text-brand-400"></i> ${data.userCount.toLocaleString('vi-VN')}`;
                document.getElementById('statAudioPlays').innerText = data.audioPlayCount.toLocaleString('vi-VN');
                document.getElementById('statPoiCount').innerText = data.poiCount.toLocaleString('vi-VN');
                document.getElementById('statTourCount').innerText = data.tourCount.toLocaleString('vi-VN');
                
                // Update Time
                const now = new Date();
                const timeStr = now.getHours().toString().padStart(2,'0') + ':' + 
                                now.getMinutes().toString().padStart(2,'0') + ':' + 
                                now.getSeconds().toString().padStart(2,'0');
                document.getElementById('lastUpdateText').innerText = 'Cập nhật lần cuối: ' + timeStr;

                // Update charts
                interactionChartIns.data.labels = data.chartLabels;
                interactionChartIns.data.datasets[0].data = data.interactionData;
                interactionChartIns.update();

                topPlacesChartIns.data.labels = data.topPlacesLabels.length > 0 ? data.topPlacesLabels : [['Chưa có', 'dữ liệu']];
                topPlacesChartIns.data.datasets[0].data = data.topPlacesData.length > 0 ? data.topPlacesData : [0];
                
                const mDataValue = Math.max(...data.topPlacesData) || 10;
                topPlacesChartIns.options.scales.x.max = mDataValue + Math.ceil(mDataValue / 4);
                topPlacesChartIns.options.scales.x.ticks.stepSize = Math.ceil(mDataValue / 4);
                topPlacesChartIns.update();
                
            } catch(e) {
                console.warn('Auto refresh failed', e);
            }
        }, 15000); // 15s refresh
    </script>
</body>
</html>