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
    const payload = req.body;
    const { sessionId, userId, deviceId, poiId, latitude, longitude, platform, appVersion } = payload;

    if (!sessionId || !latitude || !longitude) {
      return res.status(400).json({ error: 'Thiếu dữ liệu' });
    }

    const timestamp = Date.now();
    const db = admin.database();

    const getNextId = async (nodePath) => {
      const snapshot = await db.ref(nodePath).orderByKey().limitToLast(1).once('value');
      if (snapshot.exists()) {
        const lastKey = Object.keys(snapshot.val())[0];
        const nextId = parseInt(lastKey, 10);
        if (!isNaN(nextId)) {
          return nextId + 1;
        }
      }
      return 1;
    };

    let guestSessionId = null;
    if (!userId && deviceId) {
      const guestRef = db.ref('GuestSession');
      const snapshot = await guestRef.orderByChild('deviceId').equalTo(deviceId).once('value');
      if (snapshot.exists()) {
        const guests = snapshot.val();
        guestSessionId = Object.keys(guests)[0];
        await db.ref(`GuestSession/${guestSessionId}`).update({
          lastSeenAt: new Date(timestamp).toISOString()
        });
      } else {
        guestSessionId = await getNextId('GuestSession');
        await db.ref(`GuestSession/${guestSessionId}`).set({
          id: guestSessionId,
          deviceId: deviceId,
          platform: platform || 'unknown',
          appVersion: appVersion || '1.0',
          createdAt: new Date(timestamp).toISOString(),
          lastSeenAt: new Date(timestamp).toISOString(),
          convertedToUserId: null
        });
      }
    }

    let presenceId = null;
    const presenceSnap = await db.ref('UserPresence').orderByChild('sessionId').equalTo(sessionId).once('value');
    if (presenceSnap.exists()) {
      presenceId = Object.keys(presenceSnap.val())[0];
    } else {
      presenceId = await getNextId('UserPresence');
    }

    await db.ref(`UserPresence/${presenceId}`).update({
      id: parseInt(presenceId, 10) || presenceId,
      userId: userId || null,
      guestSessionId: guestSessionId,
      sessionId: sessionId,
      deviceId: deviceId || 'unknown',
      poiId: poiId || null,
      latitude: latitude,
      longitude: longitude,
      updatedAt: new Date(timestamp).toISOString(),
      platform: platform || 'MAUI',
      appVersion: appVersion || '1.0'
    });

    res.status(200).json({ status: 'success', message: 'OK' });

  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Internal Server Error' });
  }
};
