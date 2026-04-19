const admin = require('firebase-admin');

if (!admin.apps.length) {
  const serviceAccount = require('../serviceAccountKey.json');
  admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
    databaseURL: "https://vinhkhanh-68a4b-default-rtdb.asia-southeast1.firebasedatabase.app/" 
  });
}

module.exports = async (req, res) => {
  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method Not Allowed' });
  }

  try {
    const { username, passwordHash, email, phone, role } = req.body;

    if (!username || !passwordHash || !email || !phone) {
      return res.status(400).json({ error: 'Thiếu dữ liệu bắt buộc (username, passwordHash, email, phone)' });
    }

    const db = admin.database();
    
    // Check if username easily exists
    const usersRef = db.ref('User');
    const snapshot = await usersRef.orderByChild('username').equalTo(username).once('value');
    if (snapshot.exists()) {
       return res.status(409).json({ error: 'Tên đăng nhập đã tồn tại.' });
    }

    // Check if email easily exists
    const snapshotEmail = await usersRef.orderByChild('email').equalTo(email).once('value');
    if (snapshotEmail.exists()) {
       return res.status(409).json({ error: 'Email đã tồn tại.' });
    }

    // Helper để tạo ID tăng dần (1, 2, 3...)
    const getNextId = async (nodePath) => {
      const snap = await db.ref(nodePath).orderByKey().limitToLast(1).once('value');
      if (snap.exists()) {
        const lastKey = Object.keys(snap.val())[0];
        const nextId = parseInt(lastKey, 10);
        if (!isNaN(nextId)) {
          return nextId + 1;
        }
      }
      return 1;
    };

    const userId = await getNextId('User');
    const now = new Date().toISOString().replace('T', ' ').substring(0, 19);

    await db.ref(`User/${userId}`).set({
      id: userId,
      username: username,
      passwordHash: passwordHash,
      email: email,
      phone: parseInt(phone, 10) || phone,
      role: role || 'CUSTOMER',
      createdAt: now,
      updatedAt: now
    });

    res.status(200).json({ status: 'success', message: 'Đăng ký thành công', data: { id: userId, username: username } });

  } catch (error) {
    console.error('Lỗi hệ thống Worker mảng Register:', error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
