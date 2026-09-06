const { apiBase, TIMEOUT_MS, RETRY_COUNT } = require('./config');

/** 网络层可分辨的错误类型，页面据此决定是「重试」还是「去登录」。 */
const ErrorKind = {
  NETWORK: 'NETWORK',   // 断网、超时、DNS —— 可重试
  AUTH: 'AUTH',         // 401，令牌过期或失效
  BUSINESS: 'BUSINESS', // 服务端明确返回 ok:false
  SERVER: 'SERVER'      // 5xx
};

function makeError(kind, message) {
  const e = new Error(message);
  e.kind = kind;
  e.retriable = kind === ErrorKind.NETWORK || kind === ErrorKind.SERVER;
  return e;
}

function onUnauthorized() {
  wx.removeStorageSync('ft_token');
  wx.removeStorageSync('ft_token_expire');
  wx.reLaunch({ url: '/pages/login/login' });
}

function once(path, method, data) {
  const token = wx.getStorageSync('ft_token') || '';
  return new Promise((resolve, reject) => {
    wx.request({
      url: apiBase + path,
      method: method || 'GET',
      data: data || {},
      timeout: TIMEOUT_MS,
      header: {
        'content-type': 'application/json',
        Authorization: token ? 'Bearer ' + token : ''
      },
      success(res) {
        const body = res.data || {};
        if (res.statusCode === 401) {
          onUnauthorized();
          reject(makeError(ErrorKind.AUTH, body.message || '登录已过期，请重新登录'));
          return;
        }
        if (res.statusCode >= 500) {
          reject(makeError(ErrorKind.SERVER, body.message || '服务暂时不可用'));
          return;
        }
        if (res.statusCode >= 400 || body.ok === false) {
          reject(makeError(ErrorKind.BUSINESS, body.message || '请求失败'));
          return;
        }
        resolve(body);
      },
      fail(err) {
        const msg = (err && err.errMsg) || '';
        const text = /timeout/i.test(msg) ? '请求超时，请检查网络后重试' : '网络连接失败，请重试';
        reject(makeError(ErrorKind.NETWORK, text));
      }
    });
  });
}

/** GET 是幂等的，网络类失败自动重试；POST 绝不重试，避免重复提交。 */
function request(path, method, data) {
  const m = method || 'GET';
  const canRetry = m === 'GET';
  let attempt = 0;

  function run() {
    return once(path, m, data).catch((err) => {
      if (canRetry && err.retriable && attempt < RETRY_COUNT) {
        attempt++;
        return new Promise((r) => setTimeout(r, 300 * attempt)).then(run);
      }
      throw err;
    });
  }
  return run();
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
      timeout: TIMEOUT_MS * 3,   // 上传比普通请求慢
      formData: personId ? { personId: String(personId) } : {},
      header: { Authorization: token ? 'Bearer ' + token : '' },
      success(res) {
        let body = {};
        try { body = JSON.parse(res.data || '{}'); } catch (e) { body = {}; }
        if (res.statusCode === 401) {
          onUnauthorized();
          reject(makeError(ErrorKind.AUTH, '登录已过期，请重新登录'));
          return;
        }
        if (res.statusCode >= 500) {
          reject(makeError(ErrorKind.SERVER, body.message || '服务暂时不可用'));
          return;
        }
        if (res.statusCode >= 400 || body.ok === false) {
          reject(makeError(ErrorKind.BUSINESS, body.message || '上传失败'));
          return;
        }
        resolve(body);
      },
      fail(err) {
        const msg = (err && err.errMsg) || '';
        const text = /timeout/i.test(msg) ? '上传超时，请重试' : '上传失败，请检查网络';
        reject(makeError(ErrorKind.NETWORK, text));
      }
    });
  });
}

/**
 * 记住令牌与本地过期时间。服务端 ApiTokenDays 默认 30 天，
 * 原先只存令牌本身，用户会在操作中途被 401 踢出且拿不到解释。
 */
function saveLogin(data) {
  const d = (data && data.data) || data || {};
  if (d.token) {
    wx.setStorageSync('ft_token', d.token);
    const days = d.expireDays || 30;
    wx.setStorageSync('ft_token_expire', Date.now() + days * 24 * 3600 * 1000);
  }
  if (d.realName) wx.setStorageSync('ft_name', d.realName);
  wx.setStorageSync('ft_needIdCard', !!d.needIdCard);
}

/** 本地是否还持有一个未过期的令牌。用于进页面前先判断，而不是等 401。 */
function hasValidToken() {
  const token = wx.getStorageSync('ft_token');
  if (!token) return false;
  const exp = wx.getStorageSync('ft_token_expire');
  if (!exp) return true;   // 老版本存的令牌没有过期时间，交给服务端判定
  return Date.now() < exp;
}

function logout() {
  wx.removeStorageSync('ft_token');
  wx.removeStorageSync('ft_token_expire');
  wx.removeStorageSync('ft_name');
  wx.removeStorageSync('ft_needIdCard');
}

function absUrl(path) {
  if (!path) return '';
  if (/^https?:\/\//i.test(path)) return path;
  return apiBase.replace(/\/$/, '') + path;
}

module.exports = {
  apiBase, get, post, uploadPhoto, saveLogin, logout, absUrl,
  hasValidToken, ErrorKind
};
