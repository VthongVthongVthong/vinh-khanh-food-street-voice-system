<?php
session_start();
require_once 'db.php';

// Kiểm tra role, nếu không phải OWNER thì đẩy về trang login
if (!isset($_SESSION['user_id']) || strtoupper($_SESSION['role']) !== 'OWNER') {
    header("Location: login.php");
    exit;
}

$user_id = $_SESSION['user_id'];
$username = $_SESSION['username'] ?? 'Chủ cửa hàng';

// 1. Lấy danh sách POI từ SQLite
$db = new SQLiteDB();
$pdo = $db->getPDO();
$stmt = $pdo->prepare("SELECT Id, Name FROM POI WHERE ownerId = :owner_id");
$stmt->execute([':owner_id' => $user_id]);
$pois = $stmt->fetchAll(PDO::FETCH_ASSOC);

$owner_poi_ids = [];
$poi_names = [];
foreach ($pois as $poi) {
    $poiId = (int)$poi['Id'];
    $owner_poi_ids[] = $poiId;
    $poi_names[$poiId] = $poi['Name'];
}

// 2. Lấy dữ liệu Firebase
$audioPlayLogJSON = @file_get_contents("https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/AudioPlayLog.json");
$visitLogJSON = @file_get_contents("https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/VisitLog.json");

$audioPlays = $audioPlayLogJSON ? json_decode($audioPlayLogJSON, true) : [];
$visits = $visitLogJSON ? json_decode($visitLogJSON, true) : [];

if (!is_array($audioPlays)) $audioPlays = [];
if (!is_array($visits)) $visits = [];

// 3. Tính toán Thống kê
$total_audioplay = 0;
$total_visit = 0;
$total_duration = 0;
$visit_with_duration_count = 0;
$poi_performance = [];

foreach ($owner_poi_ids as $id) {
    $poi_performance[$id] = [
        'id' => $id,
        'name' => $poi_names[$id],
        'visits' => 0,
        'rating' => rand(40, 50) / 10 // Mock rating 4.0 - 5.0
    ];
}

// Chuẩn bị dữ liệu cho biểu đồ 7 ngày và 30 ngày qua
function generateDays($daysCount) {
    $weekMap = [0 => 'CN', 1 => 'T2', 2 => 'T3', 3 => 'T4', 4 => 'T5', 5 => 'T6', 6 => 'T7'];
    $days = [];
    for ($i = $daysCount - 1; $i >= 0; $i--) {
        $dateStr = date('Y-m-d', strtotime("-$i days"));
        $days[$dateStr] = [
            'label' => ($daysCount <= 7) ? $weekMap[date('w', strtotime($dateStr))] : date('d/m', strtotime($dateStr)),
            'visits' => 0,
            'audio' => 0
        ];
    }
    return $days;
}

$last7Days = generateDays(7);
$last30Days = generateDays(30);

// Process AudioPlayLog
foreach ($audioPlays as $log) {
    if (!$log || !isset($log['poiId'])) continue;
    $poiId = (int)$log['poiId'];
    if (in_array($poiId, $owner_poi_ids)) {
        $total_audioplay++;
        
        if (isset($log['playTime'])) {
            $date = substr($log['playTime'], 0, 10);
            if (isset($last7Days[$date])) $last7Days[$date]['audio']++;
            if (isset($last30Days[$date])) $last30Days[$date]['audio']++;
        }
    }
}

// Process VisitLog
foreach ($visits as $log) {
    if (!$log || !isset($log['poiId'])) continue;
    $poiId = (int)$log['poiId'];
    if (in_array($poiId, $owner_poi_ids)) {
        $total_visit++;
        
        if (isset($log['durationStayed']) && is_numeric($log['durationStayed'])) {
            $total_duration += (float)$log['durationStayed'];
            $visit_with_duration_count++;
        }
        
        if (isset($poi_performance[$poiId])) {
            $poi_performance[$poiId]['visits']++;
        }
        
        if (isset($log['visitTime'])) {
            $date = substr($log['visitTime'], 0, 10);
            if (isset($last7Days[$date])) $last7Days[$date]['visits']++;
            if (isset($last30Days[$date])) $last30Days[$date]['visits']++;
        }
    }
}

$avg_duration_minutes = $visit_with_duration_count > 0 ? round(($total_duration / $visit_with_duration_count) / 60, 1) : 0;
$saved_promo_count = 0;

// Sắp xếp hiệu suất POI theo lượt đến
usort($poi_performance, function($a, $b) {
    return $b['visits'] <=> $a['visits'];
});

$chart_labels = [];
$chart_visits = [];
$chart_audio = [];
foreach ($last7Days as $date => $data) {
    $chart_labels[] = $data['label'];
    $chart_visits[] = $data['visits'];
    $chart_audio[] = $data['audio'];
}

$chart_labels_30 = [];
$chart_visits_30 = [];
$chart_audio_30 = [];
foreach ($last30Days as $date => $data) {
    $chart_labels_30[] = $data['label'];
    $chart_visits_30[] = $data['visits'];
    $chart_audio_30[] = $data['audio'];
}

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Tổng quan doanh nghiệp - Vendor Portal</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <!-- Chart.js -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
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
                <a href="dashboard_partner.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
                    <i class="fas fa-th-large w-5 text-center"></i>
                    Tổng quan
                </a>
                <a href="index_partner.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
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
            <div class="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div>
                    <h2 class="text-2xl font-bold text-gray-800 flex items-center gap-2">
                        Chào mừng trở lại, <?php echo htmlspecialchars($username); ?> <span class="text-2xl">👋</span>
                    </h2>
                    <p class="text-sm text-gray-500 mt-1">Tổng quan hoạt động kinh doanh tại các địa điểm của bạn hôm nay.</p>
                </div>
                <a href="index_partner.php" class="bg-brand-500 hover:bg-brand-600 text-white px-5 py-2.5 rounded-lg font-medium transition-colors flex items-center gap-2 shadow-sm">
                    <i class="fas fa-plus"></i>
                    Thêm Cửa Hàng
                </a>
            </div>

            <!-- Stats Row -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <!-- Card 1 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 flex flex-col shadow-sm">
                    <div class="w-12 h-12 rounded-full bg-blue-50 text-blue-500 flex items-center justify-center text-xl mb-4">
                        <i class="fas fa-headphones-alt"></i>
                    </div>
                    <div class="flex items-end justify-between">
                        <div>
                            <div class="text-3xl font-bold text-gray-800 mb-1"><?php echo number_format($total_audioplay); ?></div>
                            <div class="text-sm text-gray-500 font-medium">Lượt Nghe Thuyết Minh</div>
                        </div>
                        <div class="text-sm font-semibold text-green-500 flex items-center gap-1">
                            +12.5% <i class="fas fa-arrow-up"></i>
                        </div>
                    </div>
                </div>

                <!-- Card 2 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 flex flex-col shadow-sm">
                    <div class="w-12 h-12 rounded-full bg-orange-50 text-brand-500 flex items-center justify-center text-xl mb-4">
                        <i class="fas fa-users"></i>
                    </div>
                    <div class="flex items-end justify-between">
                        <div>
                            <div class="text-3xl font-bold text-gray-800 mb-1"><?php echo number_format($total_visit); ?></div>
                            <div class="text-sm text-gray-500 font-medium">Khách Ghé Thăm (Ước tính)</div>
                        </div>
                        <div class="text-sm font-semibold text-green-500 flex items-center gap-1">
                            +8.2% <i class="fas fa-arrow-up"></i>
                        </div>
                    </div>
                </div>

                <!-- Card 3 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 flex flex-col shadow-sm">
                    <div class="w-12 h-12 rounded-full bg-green-50 text-green-500 flex items-center justify-center text-xl mb-4">
                        <i class="fas fa-chart-line"></i>
                    </div>
                    <div class="flex items-end justify-between">
                        <div>
                            <div class="text-3xl font-bold text-gray-800 mb-1"><?php echo number_format($saved_promo_count); ?></div>
                            <div class="text-sm text-gray-500 font-medium">Đã Lưu Mã Khuyến Mãi</div>
                        </div>
                        <div class="text-sm font-semibold text-green-500 flex items-center gap-1">
                            +24.1% <i class="fas fa-arrow-up"></i>
                        </div>
                    </div>
                </div>

                <!-- Card 4 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 flex flex-col shadow-sm">
                    <div class="w-12 h-12 rounded-full bg-purple-50 text-purple-500 flex items-center justify-center text-xl mb-4">
                        <i class="far fa-clock"></i>
                    </div>
                    <div class="flex items-end justify-between">
                        <div>
                            <div class="text-3xl font-bold text-gray-800 mb-1"><?php echo $avg_duration_minutes; ?></div>
                            <div class="text-sm text-gray-500 font-medium">Thời Gian Nán Lại (Phút)</div>
                        </div>
                        <div class="text-sm font-semibold text-red-500 flex items-center gap-1">
                            -1.2% <i class="fas fa-arrow-down"></i>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Content Grid 2/3 and 1/3 -->
            <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <!-- Chart Area -->
                <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-6 lg:col-span-2 flex flex-col">
                    <div class="flex justify-between items-center mb-6">
                        <div>
                            <h3 class="text-lg font-bold text-gray-800">Lưu Lượng Khách Tuần Này</h3>
                            <p class="text-sm text-gray-500">Lượt đến thực tế vs. Lượt nghe thuyết minh</p>
                        </div>
                        <select id="timeRangeSelect" class="px-3 py-1.5 bg-gray-50 border border-gray-200 rounded text-sm text-gray-700 outline-none hover:bg-gray-100 transition-colors cursor-pointer">
                            <option value="7">7 ngày qua</option>
                            <option value="30">30 ngày qua</option>
                        </select>
                    </div>
                    <div class="flex-1 w-full relative min-h-[300px]">
                        <canvas id="trafficChart"></canvas>
                    </div>
                </div>

                <!-- Sidebar Content -->
                <div class="flex flex-col gap-6">
                    
                    <!-- Hiệu Suất Cửa Hàng -->
                    <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-6 flex flex-col">
                        <h3 class="text-lg font-bold text-gray-800 mb-1">Hiệu Suất Cửa Hàng</h3>
                        <p class="text-sm text-gray-500 mb-5">Cửa hàng có nhiều lượt tương tác nhất</p>
                        
                        <div class="space-y-4 flex-1">
                            <?php if (empty($poi_performance)): ?>
                                <div class="text-sm text-gray-500 text-center py-4">Bạn chưa có cửa hàng nào.</div>
                            <?php else: ?>
                                <?php $count = 0; foreach ($poi_performance as $poi): if ($count >= 3) break; ?>
                                <div class="flex items-center justify-between p-3.5 bg-gray-50 rounded-lg border border-gray-100">
                                    <div class="flex flex-col">
                                        <div class="font-bold text-gray-800 text-[15px]"><?php echo htmlspecialchars($poi['name']); ?></div>
                                        <div class="text-sm text-gray-500 mt-1 flex items-center gap-1">
                                            <i class="fas fa-user-friends text-gray-400 text-xs"></i> <?php echo number_format($poi['visits']); ?> lượt đến
                                        </div>
                                    </div>
                                    <div class="flex flex-col items-end gap-2">
                                        <div class="text-sm font-bold text-orange-500 flex items-center gap-1">
                                            <i class="fas fa-star"></i> <?php echo number_format($poi['rating'], 1); ?>
                                        </div>
                                        <a href="index_partner.php?search=<?php echo urlencode($poi['name']); ?>" class="text-[11px] font-semibold text-brand-500 hover:underline">Xem chi tiết ↗</a>
                                    </div>
                                </div>
                                <?php $count++; endforeach; ?>
                            <?php endif; ?>
                        </div>
                    </div>

                    <!-- Mẹo Tăng Doanh Thu CTA -->
                    <div class="bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl shadow-md p-6 text-white relative overflow-hidden">
                        <div class="absolute top-0 right-0 -mt-4 -mr-4 w-24 h-24 bg-white/10 rounded-full blur-xl"></div>
                        <div class="absolute bottom-0 left-0 -mb-6 -ml-6 w-32 h-32 bg-indigo-900/20 rounded-full blur-2xl"></div>
                        
                        <div class="relative z-10 flex flex-col">
                            <h3 class="text-lg font-bold mb-2 flex items-center gap-2">
                                <i class="fas fa-tag"></i> Mẹo Tăng Doanh Thu
                            </h3>
                            <p class="text-sm text-indigo-100 mb-5 leading-relaxed">
                                Thêm mã giảm giá 10% vào khung giờ 19:00 - 21:00 để thu hút khách nghe thuyết minh!
                            </p>
                            <button class="w-full bg-white text-indigo-600 font-bold py-2.5 rounded-lg shadow-sm hover:bg-gray-50 transition-colors">
                                Tạo Khuyến Mãi Ngay
                            </button>
                        </div>
                    </div>

                </div>
            </div>

        </div>
    </main>

    <!-- Khởi tạo Chart.js -->
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            const ctx = document.getElementById('trafficChart');
            if (!ctx) return;
            
            const data7 = {
                labels: <?php echo json_encode($chart_labels); ?>,
                visits: <?php echo json_encode($chart_visits); ?>,
                audio: <?php echo json_encode($chart_audio); ?>
            };
            
            const data30 = {
                labels: <?php echo json_encode($chart_labels_30); ?>,
                visits: <?php echo json_encode($chart_visits_30); ?>,
                audio: <?php echo json_encode($chart_audio_30); ?>
            };
            
            let gradientFill = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
            gradientFill.addColorStop(0, 'rgba(129, 140, 248, 0.4)'); // indigo-400
            gradientFill.addColorStop(1, 'rgba(129, 140, 248, 0.0)');

            let gradientAudio = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
            gradientAudio.addColorStop(0, 'rgba(249, 115, 22, 0.4)'); // orange-500
            gradientAudio.addColorStop(1, 'rgba(249, 115, 22, 0.0)');

            let chartInstance = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: data7.labels,
                    datasets: [
                    {
                        label: 'Lượt khách tham quan',
                        data: data7.visits,
                        borderColor: '#6366f1', // indigo-500
                        borderWidth: 2,
                        backgroundColor: gradientFill,
                        fill: true,
                        tension: 0.4,
                        pointBackgroundColor: '#fff',
                        pointBorderColor: '#6366f1',
                        pointBorderWidth: 2,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    },
                    {
                        label: 'Lượt nghe thuyết minh',
                        data: data7.audio,
                        borderColor: '#f97316', // orange-500
                        borderWidth: 2,
                        backgroundColor: gradientAudio,
                        fill: true,
                        tension: 0.4,
                        pointBackgroundColor: '#fff',
                        pointBorderColor: '#f97316',
                        pointBorderWidth: 2,
                        pointRadius: 4,
                        pointHoverRadius: 6
                    }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            backgroundColor: 'rgba(255, 255, 255, 0.9)',
                            titleColor: '#1f2937',
                            bodyColor: '#4b5563',
                            borderColor: '#e5e7eb',
                            borderWidth: 1,
                            padding: 10,
                            displayColors: false,
                            callbacks: {
                                label: function(context) {
                                    return context.parsed.y + ' lượt';
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            // Xóa giới hạn scale max và step để Chart tự điều chỉnh min/max tỉ lệ thuận với 1-2 lượt
                            ticks: {
                                color: '#9ca3af',
                                font: {
                                    size: 11
                                },
                                stepSize: 1 // Ép cho số nguyên (1, 2, 3...)
                            },
                            grid: {
                                color: '#f3f4f6',
                                drawBorder: false,
                                borderDash: [5, 5]
                            }
                        },
                        x: {
                            ticks: {
                                color: '#9ca3af',
                                font: {
                                    size: 11
                                }
                            },
                            grid: {
                                display: false,
                                drawBorder: false
                            }
                        }
                    },
                    interaction: {
                        intersect: false,
                        mode: 'index',
                    },
                }
            });
            
            // Lắng nghe sự kiện chuyển ngày
            document.getElementById('timeRangeSelect').addEventListener('change', function(e) {
                const val = e.target.value;
                if(val === '7') {
                    chartInstance.data.labels = data7.labels;
                    chartInstance.data.datasets[0].data = data7.visits;
                    chartInstance.data.datasets[1].data = data7.audio;
                } else if(val === '30') {
                    chartInstance.data.labels = data30.labels;
                    chartInstance.data.datasets[0].data = data30.visits;
                    chartInstance.data.datasets[1].data = data30.audio;
                }
                chartInstance.update();
            });
        });
    </script>
</body>
</html>
