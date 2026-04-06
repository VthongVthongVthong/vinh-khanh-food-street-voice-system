<?php
class SQLiteDB {
    private $pdo;

    public function __construct() {
        try {
            $dsn = '';
            $user = '';
            $pass = '';
            
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