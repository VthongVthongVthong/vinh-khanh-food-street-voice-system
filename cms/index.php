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
                <span class="text-sm text-gray-500">Cập nhật lần cuối: <?php echo date('H:i:s'); ?></span>
            </div>

            <?php
            // Kết nối CSDL tạm (sau này sẽ query thực tế)
            require_once 'db.php';
            $db = new SQLiteDB();
            $pdo = $db->getPDO();
            
            // Query thử số lượng POI
            $poiCount = 0;
            try {
                $stmt = $pdo->query("SELECT COUNT(*) FROM POIs WHERE IsActive = 1");
                $poiCount = $stmt->fetchColumn();
            } catch (Exception $e) {
                // Ignore if table doesn't exist yet
            }
            ?>

            <!-- Stats Grid -->
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                <!-- Stat Card 1 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tổng lượt người dùng</p>
                            <h3 class="text-2xl font-bold text-gray-800">45.231</h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-users"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-green-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-trend-up"></i> +12.5%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>

                <!-- Stat Card 2 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Tổng lượt phát Audio</p>
                            <h3 class="text-2xl font-bold text-gray-800">89.450</h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-green-50 text-green-600 flex items-center justify-center">
                            <i class="fas fa-headphones"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-green-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-trend-up"></i> +8.2%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>

                <!-- Stat Card 3 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm hover:shadow-md transition-shadow">
                    <div class="flex justify-between items-start">
                        <div>
                            <p class="text-sm font-medium text-gray-500 mb-1">Điểm POI Active</p>
                            <h3 class="text-2xl font-bold text-gray-800"><?php echo $poiCount > 0 ? $poiCount : '20'; ?></h3>
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
                            <h3 class="text-2xl font-bold text-gray-800">1.234</h3>
                        </div>
                        <div class="w-10 h-10 rounded-lg bg-red-50 text-red-500 flex items-center justify-center">
                            <i class="fas fa-route"></i>
                        </div>
                    </div>
                    <div class="mt-4 flex items-center text-sm">
                        <span class="text-red-500 font-medium flex items-center gap-1">
                            <i class="fas fa-arrow-trend-down"></i> -2.4%
                        </span>
                        <span class="text-gray-400 ml-2">so với tuần trước</span>
                    </div>
                </div>
            </div>

            <!-- Charts Section -->
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
                <!-- Line Chart -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                    <h3 class="text-lg font-bold text-gray-800 mb-4">Lượt tương tác theo tuần</h3>
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
        // Cấu hình chung cho Chart.js
        Chart.defaults.font.family = "'Inter', 'Segoe UI', sans-serif";
        Chart.defaults.color = '#9CA3AF';
        
        // Line Chart: Lượt tương tác
        const ctxInteraction = document.getElementById('interactionChart').getContext('2d');
        new Chart(ctxInteraction, {
            type: 'line',
            data: {
                labels: ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'],
                datasets: [{
                    label: 'Lượt tương tác',
                    data: [4000, 3000, 4500, 5200, 8900, 12200, 11000],
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
                        max: 15000,
                        ticks: { stepSize: 3000 },
                        grid: { borderDash: [4, 4], color: '#F3F4F6', drawBorder: false }
                    },
                    x: {
                        grid: { display: false, drawBorder: false }
                    }
                }
            }
        });

        // Bar Chart: Top địa điểm (Ngang)
        const ctxTopPlaces = document.getElementById('topPlacesChart').getContext('2d');
        new Chart(ctxTopPlaces, {
            type: 'bar',
            data: {
                labels: [
                    ['Ốc', 'Oanh'], 
                    ['Chè Mâm', 'Vĩnh Khánh'], 
                    ['Phá Lấu', 'Bò Cô Thảo'], 
                    ['Bạch Tuộc', 'Nướng Dì Hai']
                ],
                datasets: [{
                    data: [14200, 11800, 9200, 3500],
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
                        max: 16000,
                        ticks: { stepSize: 4000 },
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
    </script>
</body>
</html>