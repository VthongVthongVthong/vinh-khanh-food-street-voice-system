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
    <link rel="icon" type="image/png" href="img/icon.png">
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vĩnh Khánh CMS - Phân tích Heatmap</title>
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
                <a href="tour.php" class="flex items-center gap-3 px-4 py-3 text-gray-600 hover:bg-gray-50 hover:text-gray-900 rounded-lg font-medium transition-colors">
                    <i class="fas fa-route w-5 text-center"></i>
                    Quản lý Tour
                </a>
                <a href="heatmap.php" class="flex items-center gap-3 px-4 py-3 bg-brand-50 text-brand-600 rounded-lg font-medium transition-colors">
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
                <div>
                    <h2 class="text-2xl font-bold text-gray-800 flex items-center gap-2">
                        <i class="fas fa-chart-line text-brand-500"></i> Phân Tích Heatmap & Hành Vi
                    </h2>
                    <p class="text-sm text-gray-500 mt-1">Theo dõi mật độ khách và tương tác audio theo thời gian thực</p>
                    <p id="lastUpdateText" class="text-xs text-brand-500 mt-1">Cập nhật lần cuối: <?php echo date('H:i:s'); ?></p>
                </div>
                
                <div class="flex items-center flex-wrap gap-2">
                    <div class="inline-flex rounded-md shadow-sm" role="group">
                        <button type="button" id="btnToday" onclick="setGlobalFilter('today')" class="px-4 py-2 text-sm font-medium text-brand-600 bg-gray-100 border border-gray-200 rounded-l-lg hover:bg-gray-100 focus:z-10 focus:ring-2 focus:ring-brand-500 transition-colors">
                            Hôm nay
                        </button>
                        <button type="button" id="btnThisWeek" onclick="setGlobalFilter('week')" class="px-4 py-2 text-sm font-medium text-gray-900 bg-white border-t border-b border-gray-200 hover:bg-gray-100 focus:z-10 focus:ring-2 focus:ring-brand-500 transition-colors">
                            Tuần này
                        </button>
                        <button type="button" id="btnThisMonth" onclick="setGlobalFilter('month')" class="px-4 py-2 text-sm font-medium text-gray-900 bg-white border border-gray-200 rounded-r-lg hover:bg-gray-100 focus:z-10 focus:ring-2 focus:ring-brand-500 transition-colors">
                            Tháng này
                        </button>
                    </div>
                    
                    <div class="relative flex items-center">
                        <input type="date" id="globalDatePicker" title="Chọn ngày cụ thể" class="p-2 bg-white border border-gray-200 rounded-lg text-gray-600 hover:bg-gray-50 focus:ring-2 focus:ring-brand-500 outline-none cursor-pointer" value="<?php echo date('Y-m-d'); ?>" onchange="setCustomDateFilter(this.value)">
                    </div>

                    <button class="p-2 bg-white border border-gray-200 rounded-lg text-gray-600 hover:bg-gray-50" title="Tải xuống báo cáo"><i class="fas fa-download"></i></button>
                </div>
            </div>

            <!-- Stats Grid -->
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
                <!-- Card 1 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm flex flex-col justify-between relative overflow-hidden">
                    <div class="flex justify-between items-start z-10 w-full mb-4">
                        <div class="flex items-center gap-2 text-brand-500 font-medium text-sm">
                            <i class="fas fa-user-friends"></i> Tổng Sessions (Guest & User)
                        </div>
                        <i class="fas fa-user-friends text-4xl text-gray-100 absolute right-4 top-4"></i>
                    </div>
                    <div class="z-10">
                        <h3 class="text-3xl font-bold text-gray-800" id="totalSessions">0</h3>
                        <p class="text-sm mt-2 text-green-500 font-medium flex items-center gap-1"><i class="fas fa-bolt"></i> Đang active: <span id="activeSessions">0</span></p>
                    </div>
                </div>

                <!-- Card 2 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm flex flex-col justify-between relative overflow-hidden">
                    <div class="flex justify-between items-start z-10 w-full mb-4">
                        <div class="flex items-center gap-2 text-orange-500 font-medium text-sm">
                            <i class="far fa-clock"></i> Thời Gian Lưu Trú TB
                        </div>
                        <i class="far fa-clock text-4xl text-gray-100 absolute right-4 top-4"></i>
                    </div>
                    <div class="z-10">
                        <h3 class="text-3xl font-bold text-gray-800"><span id="avgDuration">0</span> <span class="text-lg font-normal text-gray-500">phút</span></h3>
                        <p class="text-sm mt-2 text-green-500 font-medium flex items-center gap-1"><i class="fas fa-arrow-up"></i> +12% so với tuần trước</p>
                    </div>
                </div>

                <!-- Card 3 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm flex flex-col justify-between relative overflow-hidden">
                    <div class="flex justify-between items-start z-10 w-full mb-4">
                        <div class="flex items-center gap-2 text-green-500 font-medium text-sm">
                            <i class="fas fa-headphones"></i> Audio Hoàn Thành
                        </div>
                        <i class="fas fa-headphones text-4xl text-gray-100 absolute right-4 top-4"></i>
                    </div>
                    <div class="z-10">
                        <h3 class="text-3xl font-bold text-gray-800"><span id="audioCompletion">0</span>%</h3>
                        <p class="text-sm mt-2 text-gray-500"><span id="totalAudioPlays">0</span> lượt nghe tổng cộng</p>
                    </div>
                </div>

                <!-- Card 4 -->
                <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm flex flex-col justify-between relative overflow-hidden">
                    <div class="flex justify-between items-start z-10 w-full mb-4">
                        <div class="flex items-center gap-2 text-purple-500 font-medium text-sm">
                            <i class="far fa-map"></i> POI Kích Hoạt (AUTO)
                        </div>
                        <i class="far fa-map text-4xl text-gray-100 absolute right-4 top-4"></i>
                    </div>
                    <div class="z-10">
                        <h3 class="text-3xl font-bold text-gray-800" id="poiAutoTriggers">0</h3>
                        <p class="text-sm mt-2 text-gray-500 uppercase tracking-widest text-[10px] font-bold">MANUAL: <span id="poiManualTriggers">0</span></p>
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <!-- Heatmap Chart -->
                <div class="lg:col-span-2 bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                    <div class="flex justify-between items-start mb-4">
                        <div>
                            <h3 class="text-lg font-bold text-gray-800">Bản Đồ Mật Độ Phố Vĩnh Khánh</h3>
                            <p class="text-xs text-gray-500">Mô phỏng phân bố vị trí khách lưu trú dựa trên tọa độ GPS</p>
                        </div>
                        <div class="flex gap-2 text-xs font-medium text-gray-600 bg-gray-50 px-3 py-1.5 rounded-lg border border-gray-100">
                            <span class="flex items-center gap-1"><span class="w-2 h-2 rounded-full bg-brand-500"></span> Cao</span>
                            <span class="flex items-center gap-1 ml-2"><span class="w-2 h-2 rounded-full bg-yellow-400"></span> TB</span>
                            <span class="flex items-center gap-1 ml-2"><span class="w-2 h-2 rounded-full bg-blue-400"></span> Thấp</span>
                        </div>
                    </div>
                    <div class="h-[300px] w-full relative border border-gray-100 rounded-lg overflow-hidden bg-gray-50">
                        <canvas id="densityChart"></canvas>
                    </div>
                </div>

                <div class="grid grid-rows-2 gap-6">
                    <!-- Platform Chart -->
                    <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                        <h3 class="text-sm font-bold text-gray-800 mb-4">Nền Tảng Guest Session</h3>
                        <div class="space-y-4" id="platformStats">
                            <!-- Injected via JS -->
                        </div>
                    </div>

                    <!-- Language Chart -->
                    <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                        <h3 class="text-sm font-bold text-gray-800 mb-4">Ngôn Ngữ Audio (AudioPlayLog)</h3>
                        <div class="h-[150px] w-full relative">
                            <canvas id="languageChart"></canvas>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Area Chart -->
            <div class="bg-white rounded-xl border border-gray-100 p-6 shadow-sm">
                <div class="flex justify-between items-start mb-4">
                    <div>
                        <h3 class="text-lg font-bold text-gray-800">Hoạt Động Theo Khung Giờ</h3>
                        <p class="text-xs text-gray-500">Mật độ khách và lượt nghe audio trong thời gian lọc</p>
                    </div>
                </div>
                <div class="h-64 w-full relative mt-2">
                    <canvas id="hourlyChart"></canvas>
                </div>
            </div>

        </div>
    </main>

    <script>
        // Cấu hình chung Chart.js
        Chart.defaults.font.family = "'Inter', 'Segoe UI', sans-serif";
        Chart.defaults.color = '#9CA3AF';

        // Fake Data cho Charts tương tự hình
        
        // 1. Bản Đồ Mật Độ (Scatter / Bubble) - Data will be replaced by Firebase
        const ctxDensity = document.getElementById('densityChart').getContext('2d');
        const densityChartIns = new Chart(ctxDensity, {
            type: 'bubble',
            data: {
                datasets: [{
                    label: 'Hoạt động',
                    data: [],
                    backgroundColor: 'rgba(255, 77, 21, 0.7)',
                    borderColor: 'rgba(255, 77, 21, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: { 
                        min: 106.699, max: 106.706,
                        grid: { color: '#F3F4F6' }
                    },
                    y: { 
                        min: 10.757, max: 10.762,
                        grid: { color: '#F3F4F6' }
                    }
                }
            }
        });

        // 2. Ngôn ngữ Audio (Horizontal Bar)
        const ctxLang = document.getElementById('languageChart').getContext('2d');
        const langChartIns = new Chart(ctxLang, {
            type: 'bar',
            data: {
                labels: ['Tiếng Việt (vi)', 'English (en)', '中文 (zh)', '日本語 (ja)'],
                datasets: [{
                    data: [8, 3, 1, 1],
                    backgroundColor: [
                        '#FF4D15',
                        '#3B82F6',
                        '#10B981',
                        '#EF4444'
                    ],
                    borderRadius: 4,
                    barThickness: 16
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: { display: false, max: 10 },
                    y: { 
                        grid: { display: false, drawBorder: false },
                        ticks: { font: { size: 10 } }
                    }
                }
            }
        });

        // 3. Hoạt Động Theo Khung Giờ (Area)
        const ctxHourly = document.getElementById('hourlyChart').getContext('2d');
        
        let gradient = ctxHourly.createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, 'rgba(59, 130, 246, 0.6)');
        gradient.addColorStop(1, 'rgba(59, 130, 246, 0.05)');

        const hourlyChartIns = new Chart(ctxHourly, {
            type: 'line',
            data: {
                labels: ['18:00', '19:00', '20:00', '21:00', '22:00'],
                datasets: [{
                    label: 'Hoạt động',
                    data: [0, 0, 0, 0, 0],
                    borderColor: '#3B82F6',
                    backgroundColor: gradient,
                    borderWidth: 2,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 0,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { borderDash: [4, 4], color: '#F3F4F6', drawBorder: false }
                    },
                    x: {
                        grid: { display: false, drawBorder: false }
                    }
                }
            }
        });

        // Khai báo biến toàn cục lấy data 1 lần
        let FB_DATA = { guest: {}, audio: {}, presence: {}, visit: {} };
        let currentFilter = 'today';

        function checkDateFilter(timestamp, filter) {
            const now = new Date();
            const date = new Date(timestamp);
            
            now.setHours(0,0,0,0);
            const dateHoursZero = new Date(date).setHours(0,0,0,0);
            
            if (filter === 'today') {
                return date >= now;
            } else if (filter === 'week') {
                const day = now.getDay();
                const diff = now.getDate() - day + (day == 0 ? -6:1);
                const startOfWeek = new Date(new Date(now).setDate(diff));
                startOfWeek.setHours(0,0,0,0);
                return date >= startOfWeek;
            } else if (filter === 'month') {
                const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
                return date >= startOfMonth;
            } else if (filter === 'custom') {
                const customDateEl = document.getElementById('globalDatePicker').value;
                if (!customDateEl) return true;
                
                // Adjust for local time zone comparison
                const [y, m, d] = customDateEl.split('-');
                const targetDate = new Date(y, m - 1, d);
                return dateHoursZero === targetDate.getTime();
            }
            return true;
        }

        function setCustomDateFilter(val) {
            if(!val) return;
            currentFilter = 'custom';
            
            // Un-highlight all global filter buttons
            const activeClasses = ['text-brand-600', 'bg-gray-100'];
            const inactiveClasses = ['text-gray-900', 'bg-white'];
            
            ['btnToday', 'btnThisWeek', 'btnThisMonth'].forEach(id => {
                const el = document.getElementById(id);
                el.classList.remove(...activeClasses);
                el.classList.add(...inactiveClasses);
            });
            
            updateDashboard();
            updateHourlyChart();
        }

        // Tải dữ liệu Firebase thực tế
        async function loadFirebaseData() {
            try {
                // Fetch All Data
                const guestUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/GuestSession.json";
                const audioUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/AudioPlayLog.json";
                const presenceUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/UserPresence.json";
                const visitUrl = "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/VisitLog.json";

                const [guestRes, audioRes, presenceRes, visitRes] = await Promise.all([
                    fetch(guestUrl),
                    fetch(audioUrl),
                    fetch(presenceUrl),
                    fetch(visitUrl)
                ]);

                FB_DATA.guest = await guestRes.json() || {};
                FB_DATA.audio = await audioRes.json() || {};
                FB_DATA.presence = await presenceRes.json() || {};
                FB_DATA.visit = await visitRes.json() || {};

                updateDashboard();
                updateHourlyChart();

            } catch (err) {
                console.error("Error loading heatmap data: ", err);
            }
        }
        
        loadFirebaseData();
        
        // Cập nhật ngầm mỗi 15 giây
        setInterval(async () => {
            try {
                await loadFirebaseData();
                const now = new Date();
                const timeStr = now.getHours().toString().padStart(2,'0') + ':' + 
                                now.getMinutes().toString().padStart(2,'0') + ':' + 
                                now.getSeconds().toString().padStart(2,'0');
                const lastUpdateEl = document.getElementById('lastUpdateText');
                if (lastUpdateEl) lastUpdateEl.innerText = 'Cập nhật lần cuối: ' + timeStr;
            } catch(e) {
                console.warn('Auto refresh failed', e);
            }
        }, 15000);

        function setGlobalFilter(filter) {
            currentFilter = filter;
            
            // Update UI buttons
            const activeClasses = ['text-brand-600', 'bg-gray-100'];
            const inactiveClasses = ['text-gray-900', 'bg-white'];
            
            // Reset all
            ['btnToday', 'btnThisWeek', 'btnThisMonth'].forEach(id => {
                const el = document.getElementById(id);
                el.classList.remove(...activeClasses);
                el.classList.add(...inactiveClasses);
            });

            // Active clicked
            let activeBtn;
            if(filter === 'today') activeBtn = document.getElementById('btnToday');
            if(filter === 'week') activeBtn = document.getElementById('btnThisWeek');
            if(filter === 'month') activeBtn = document.getElementById('btnThisMonth');
            
            if(activeBtn) {
                activeBtn.classList.remove(...inactiveClasses);
                activeBtn.classList.add(...activeClasses);
            }

            // Sync date picker
            if(filter === 'today') {
                const now = new Date();
                const todayStr = new Date(now.getTime() - (now.getTimezoneOffset() * 60000)).toISOString().split('T')[0];
                document.getElementById('globalDatePicker').value = todayStr;
            } else {
                document.getElementById('globalDatePicker').value = '';
            }

            updateDashboard();
            updateHourlyChart();
        }

        function updateHourlyChart() {
            const hourlyCounts = { '18:00': 0, '19:00': 0, '20:00': 0, '21:00': 0, '22:00': 0 };
            
            Object.values(FB_DATA.audio).forEach(a => {
                if(!a || !a.playTime) return;
                const dt = new Date(a.playTime);
                if (checkDateFilter(dt, currentFilter)) {
                    const hour = dt.getHours();
                    if(hour >= 18 && hour <= 22) {
                        hourlyCounts[hour + ':00'] = (hourlyCounts[hour + ':00'] || 0) + 1;
                    }
                }
            });
            
            Object.values(FB_DATA.visit).forEach(v => {
                if(!v || !v.visitTime) return;
                const dt = new Date(v.visitTime);
                if (checkDateFilter(dt, currentFilter)) {
                    const hour = dt.getHours();
                    if(hour >= 18 && hour <= 22) {
                        hourlyCounts[hour + ':00'] = (hourlyCounts[hour + ':00'] || 0) + 1;
                    }
                }
            });
            
            hourlyChartIns.data.datasets[0].data = Object.values(hourlyCounts);
            hourlyChartIns.update();
        }

        function updateDashboard() {
                const guestData = FB_DATA.guest;
                const audioData = FB_DATA.audio;
                const presenceData = FB_DATA.presence;
                const visitData = FB_DATA.visit;

                // --- 1. Tính tổng session và platform ---
                let totalSessions = 0;
                let activeCount = 0;
                const platformCounts = { ios: 0, android: 0, web: 0 };
                
                const now = Date.now();
                const ACTIVE_THRESHOLD = 30 * 60 * 1000; // 30 phút

                let totalDuration = 0;
                let sessionsWithDuration = 0;

                Object.values(guestData).forEach(guest => {
                    if (!guest) return;
                    let createdObj = guest.createdAt || guest.timestamp;
                    if(createdObj) {
                        let cDate = new Date(createdObj);
                        if(!checkDateFilter(cDate, currentFilter)) return;
                    }

                    totalSessions++;
                    
                    // Check active
                    let lastActive = guest.lastSeenAt || guest.lastActive || guest.timestamp || 0;
                    if (typeof lastActive === 'string') lastActive = new Date(lastActive).getTime();
                    else lastActive = 0; // Default if missing
                    
                    if (lastActive > 0 && (now - lastActive < ACTIVE_THRESHOLD)) {
                        activeCount++;
                    }

                    // Platform
                    const plat = (guest.platform || guest.deviceInfo || '').toLowerCase();
                    if (plat.includes('iphone') || plat.includes('ios') || plat.includes('mac')) platformCounts.ios++;
                    else if (plat.includes('android')) platformCounts.android++;
                    else platformCounts.web++;
                });

                // Calculate duration from VisitLog instead
                Object.values(visitData).forEach(visit => {
                    if (!visit) return;
                    let vDate = new Date(visit.visitTime);
                    if (!checkDateFilter(vDate, currentFilter)) return;

                    const durationStr = visit.durationStayed;
                    if (durationStr && !isNaN(parseFloat(durationStr))) {
                        totalDuration += parseFloat(durationStr);
                        sessionsWithDuration++;
                    }
                });

                document.getElementById('totalSessions').innerText = totalSessions;
                document.getElementById('activeSessions').innerText = activeCount;
                
                if (sessionsWithDuration > 0) {
                    document.getElementById('avgDuration').innerText = (totalDuration / sessionsWithDuration / 60).toFixed(1);
                } else {
                    document.getElementById('avgDuration').innerText = "0.0";
                }

                // Render platforms
                const platColors = { ios: 'bg-gray-800', android: 'bg-green-500', web: 'bg-blue-500' };
                const platIcons = { ios: 'fab fa-apple', android: 'fab fa-android', web: 'fas fa-globe' };
                const platNames = { ios: 'iOS', android: 'Android', web: 'Web' };
                
                let platHtml = '';
                ['ios', 'android', 'web'].forEach(p => {
                    if (parseFloat(totalSessions) > 0) {
                        const count = platformCounts[p];
                        const pct = count > 0 ? Math.round((count / totalSessions) * 100) : 0;
                        platHtml += `
                            <div>
                                <div class="flex justify-between text-xs font-bold text-gray-700 mb-1">
                                    <span><i class="${platIcons[p]} mr-1 w-4 text-center"></i> ${platNames[p]}</span>
                                    <span>${count} sessions (${pct}%)</span>
                                </div>
                                <div class="w-full bg-gray-100 rounded-full h-1.5">
                                    <div class="${platColors[p]} h-1.5 rounded-full" style="width: ${pct}%"></div>
                                </div>
                            </div>
                        `;
                    }
                });
                document.getElementById('platformStats').innerHTML = platHtml;

                // --- 2. Tính Audio ---
                let totalAudio = 0;
                let completedCount = 0;
                const langCounts = {};

                Object.values(audioData).forEach(audio => {
                    if (!audio) return;
                    let aDate = new Date(audio.playTime);
                    if (!checkDateFilter(aDate, currentFilter)) return;

                    totalAudio++;

                    // Count languages
                    const lang = audio.language || audio.languageCode || 'vi';
                    langCounts[lang] = (langCounts[lang] || 0) + 1;

                    // Real completion logic using completionRate
                    const cRate = parseFloat(audio.completionRate || 0);
                    if (cRate >= 90.0 || audio.completed || (audio.durationListened && audio.durationListened > 10)) {
                        completedCount++;
                    }
                });

                document.getElementById('totalAudioPlays').innerText = totalAudio;
                const pctFinish = totalAudio > 0 ? ((completedCount / totalAudio) * 100).toFixed(1) : 0;
                document.getElementById('audioCompletion').innerText = pctFinish;

                // Cập nhật biểu đồ Language
                const sortedLangs = Object.keys(langCounts).sort((a,b) => langCounts[b] - langCounts[a]);
                if (sortedLangs.length > 0) {
                    const lData = [];
                    const lLabels = [];
                    sortedLangs.slice(0, 4).forEach(k => {
                        let displayName = k;
                        if(k === 'vi') displayName = 'Tiếng Việt (vi)';
                        else if(k === 'en') displayName = 'English (en)';
                        else if(k === 'zh') displayName = '中文 (zh)';
                        else if(k === 'ja') displayName = '日本語 (ja)';
                        
                        lLabels.push(displayName);
                        lData.push(langCounts[k]);
                    });
                    
                    langChartIns.data.labels = lLabels;
                    langChartIns.data.datasets[0].data = lData;
                    langChartIns.update();
                }

                // --- 3. Đếm POI Triggers & Render Heatmap ---
                let autoTriggers = 0;
                let manualTriggers = 0;
                const heatmapCoords = [];

                // Use VisitLog for accurate triggers and also add to heatmap
                Object.values(visitData).forEach(visit => {
                    if (!visit) return;
                    let vDate = new Date(visit.visitTime);
                    if (!checkDateFilter(vDate, currentFilter)) return;

                    if (visit.triggerType === 'AUTO') autoTriggers++;
                    else if (visit.triggerType === 'MANUAL') manualTriggers++;

                    const lat = parseFloat(visit.latitude);
                    const lng = parseFloat(visit.longitude);
                    if (!isNaN(lat) && !isNaN(lng)) {
                        heatmapCoords.push({x: lng, y: lat, r: 12});
                    }
                });

                document.getElementById('poiAutoTriggers').innerText = autoTriggers;
                document.getElementById('poiManualTriggers').innerText = manualTriggers;

                // Also use UserPresence for heatmap
                Object.values(presenceData).forEach(user => {
                    if (!user) return;
                    let uDate = new Date(user.updatedAt);
                    if (!checkDateFilter(uDate, currentFilter)) return;

                    const lat = parseFloat(user.latitude);
                    const lng = parseFloat(user.longitude);
                    if (!isNaN(lat) && !isNaN(lng)) {
                        heatmapCoords.push({x: lng, y: lat, r: 15});
                    }
                });

                if (heatmapCoords.length > 0) {
                    densityChartIns.data.datasets[0].data = heatmapCoords;
                    densityChartIns.update();
                } else {
                    densityChartIns.data.datasets[0].data = [];
                    densityChartIns.update();
                }
        }

        // Tải dữ liệu Firebase thực tế
    </script>
</body>
</html>