<?php
session_start();
require_once 'db.php';

$error = '';

if ($_SERVER["REQUEST_METHOD"] == "POST") {
    $username = $_POST['username'] ?? '';
    // Xử lý password tùy theo cách bạn lưu trong DB (kèm MD5, SHA1 hoặc BCRYPT)
    $password = $_POST['password'] ?? ''; 

    if (empty($username) || empty($password)) {
        $error = 'Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!';
    } else {
        $db = new SQLiteDB(); // Dù tên class là SQLiteDB nhưng bên trong đang connect MySQL
        $pdo = $db->getPDO();

        try {
            // Giả định bảng User có các cột: username, password, role
            $stmt = $pdo->prepare("SELECT * FROM User WHERE username = :username LIMIT 1");
            $stmt->execute([':username' => $username]);
            $user = $stmt->fetch(PDO::FETCH_ASSOC);

            if ($user) {
                // Kiểm tra mật khẩu với hàm password_verify và cột passwordHash
                if (password_verify($password, $user['passwordHash'])) {
                    $role = strtoupper($user['role']);
                    if ($role === 'ADMIN' || $role === 'OWNER') {
                        // Đăng nhập thành công
                        // Lấy ID dùng mọi case (mysql fetch_assoc là case-sensitive)
                        $_SESSION['user_id'] = $user['Id'] ?? $user['id'] ?? $user['ID'] ?? $user['userId'] ?? 0;
                        $_SESSION['username'] = $user['Username'] ?? $user['username'] ?? '';
                        $_SESSION['role'] = $role;
                        
                        if ($role === 'ADMIN') {
                            header("Location: index.php");
                        } else {
                            header("Location: index_partner.php");
                        }
                        exit;
                    } else {
                        $error = 'Bạn không có quyền truy cập vào trang này. (Yêu cầu quyền ADMIN hoặc OWNER)';
                    }
                } else {
                    $error = 'Mật khẩu không chính xác!';
                }
            } else {
                $error = 'Tài khoản không tồn tại!';
            }
        } catch (Exception $e) {
            $error = 'Lỗi hệ thống: ' . $e->getMessage();
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Đăng nhập CMS - Vĩnh Khánh</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" rel="stylesheet">
    <script>
        tailwind.config = {
            theme: {
                extend: {
                    colors: {
                        primary: '#FF4D15',
                        brand: {
                            50: '#fff1ec',
                            600: '#FF4D15',
                        }
                    },
                    fontFamily: {
                        sans: ['Inter', 'Segoe UI', 'sans-serif'],
                    }
                }
            }
        }
    </script>
</head>
<body class="bg-gray-50 flex items-center justify-center h-screen">

    <div class="bg-white p-8 rounded-xl shadow-md w-full max-w-md border border-gray-100">
        <div class="text-center mb-8">
            <h1 class="text-2xl font-bold text-primary flex items-center justify-center gap-2">
                <i class="fas fa-map-marked-alt"></i> Vĩnh Khánh CMS
            </h1>
            <p class="text-gray-500 mt-2 text-sm">Đăng nhập bằng tài khoản quản trị viên</p>
        </div>

        <?php if (!empty($error)): ?>
            <div class="bg-red-50 text-red-600 p-3 rounded-lg text-sm mb-4 border border-red-200">
                <i class="fas fa-exclamation-circle mr-1"></i> <?php echo htmlspecialchars($error); ?>
            </div>
        <?php endif; ?>

        <form action="login.php" method="POST" class="space-y-5">
            <div>
                <label for="username" class="block text-sm font-medium text-gray-700 mb-1">Tên đăng nhập</label>
                <div class="relative">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <i class="fas fa-user text-gray-400"></i>
                    </div>
                    <input type="text" id="username" name="username" required
                        class="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-primary focus:border-primary sm:text-sm transition-colors"
                        placeholder="Nhập tên đăng nhập...">
                </div>
            </div>

            <div>
                <label for="password" class="block text-sm font-medium text-gray-700 mb-1">Mật khẩu</label>
                <div class="relative">
                    <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <i class="fas fa-lock text-gray-400"></i>
                    </div>
                    <input type="password" id="password" name="password" required
                        class="block w-full pl-10 pr-3 py-2 border border-gray-300 rounded-lg focus:ring-primary focus:border-primary sm:text-sm transition-colors"
                        placeholder="Nhập mật khẩu...">
                </div>
            </div>

            <button type="submit"
                class="w-full flex justify-center py-2.5 px-4 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-primary hover:bg-orange-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary transition-colors">
                <i class="fas fa-sign-in-alt mr-2 mt-0.5"></i> Đăng nhập
            </button>
        </form>
    </div>

</body>
</html>
