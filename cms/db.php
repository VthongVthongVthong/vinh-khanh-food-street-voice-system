<?php
class SQLiteDB {
    private $pdo;

    public function __construct() {
        try {
            $dsn = 'mysql:host=sql105.infinityfree.com;dbname=if0_41569426_vinhkhanh;charset=utf8mb4';
            $user = 'if0_41569426';
            $pass = 'O4c2flCiE1tZkS';
            
            $this->pdo = new PDO($dsn, $user, $pass);
            $this->pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        } catch(PDOException $e) {
            echo "Lỗi kết nối CSDL: " . $e->getMessage();
        }
    }

    public function getPDO() {
        return $this->pdo;
    }
}
?>