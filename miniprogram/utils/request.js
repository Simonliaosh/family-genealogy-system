const { apiBase } = require('./config');

function request(path, method, data) {
  const token = wx.getStorageSync('ft_token') || '';
  return new Promise((resolve, reject) => {
    wx.request({
      url: apiBase + path,
      method: method || 'GET',
      data: data || {},
      header: {
        'content-type': 'application/json',
        Authorization: token ? 'Bearer ' + token : ''
      },
      success(res) {
        const body = res.data || {};
        if (res.statusCode === 401) {
          wx.removeStorageSync('ft_token');
          wx.reLaunch({ url: '/pages/login/login' });
          reject(new Error(body.message || '未登录'));
          return;
        }
        if (res.statusCode >= 400 || body.ok === false) {
          reject(new Error(body.message || '请求失败'));
          return;
        }
        resolve(body);
      },
      fail(err) {
        reject(new Error((err && err.errMsg) || '网络失败，请检查 apiBase 与后端是否启动'));
      }
    });
  });
}

function get(path, data) {
  return request(path, 'GET', data);
}

function post(path, data) {
  return request(path, 'POST', data);
}

function uploadPhoto(filePath, personId) {
  const token = wx.getStorageSync('ft_token') || '';
  return new Promise((resolve, reject) => {
    wx.uploadFile({
      url: apiBase + '/api/FtProfile/UploadPhoto',
      filePath,
      name: 'file',
      formData: personId ? { personId: String(personId) } : {},
      header: { Authorization: token ? 'Bearer ' + token : '' },
      success(res) {
        let body = {};
        try { body = JSON.parse(res.data || '{}'); } catch (e) { body = {}; }
        if (res.statusCode === 401) {
          wx.removeStorageSync('ft_token');
          wx.reLaunch({ url: '/pages/login/login' });
          reject(new Error('未登录'));
          return;
        }
        if (body.ok === false) {
          reject(new Error(body.message || '上传失败'));
          return;
        }
        resolve(body);
      },
      fail(err) {
        reject(new Error((err && err.errMsg) || '上传失败'));
      }
    });
  });
}

function saveLogin(data) {
  const d = (data && data.data) || data || {};
  if (d.token) wx.setStorageSync('ft_token', d.token);
  if (d.realName) wx.setStorageSync('ft_name', d.realName);
  wx.setStorageSync('ft_needIdCard', !!d.needIdCard);
}

function logout() {
  wx.removeStorageSync('ft_token');
  wx.removeStorageSync('ft_name');
  wx.removeStorageSync('ft_needIdCard');
}

function absUrl(path) {
  if (!path) return '';
  if (/^https?:\/\//i.test(path)) return path;
  return apiBase.replace(/\/$/, '') + path;
}

module.exports = { apiBase, get, post, uploadPhoto, saveLogin, logout, absUrl };
