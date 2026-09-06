const api = require('../../utils/request');
const v = require('../../utils/validate');

Page({
  data: {
    wechatEnabled: false,
    configError: '',   // 取配置失败时告诉用户为什么没有微信按钮
    idCard: '',
    password: '',
    realName: '',
    regId: '',
    regPwd: '',
    regPwd2: '',
    busy: false
  },

  onShow() {
    if (api.hasValidToken()) {
      wx.switchTab({ url: '/pages/home/home' });
      return;
    }
    this.loadConfig();
  },

  loadConfig() {
    this.setData({ configError: '' });
    api.get('/api/FtAuth/Config').then(res => {
      const d = res.data || {};
      this.setData({ wechatEnabled: !!d.wechatEnabled });
    }).catch(err => {
      // 原先是 .catch(() => {})：配置取不到时微信按钮干脆不渲染，用户得不到任何解释
      this.setData({ wechatEnabled: false, configError: err.message || '无法连接服务器' });
    });
  },

  onRetryConfig() { this.loadConfig(); },

  onId(e) { this.setData({ idCard: e.detail.value }); },
  onPwd(e) { this.setData({ password: e.detail.value }); },
  onName(e) { this.setData({ realName: e.detail.value }); },
  onRegId(e) { this.setData({ regId: e.detail.value }); },
  onRegPwd(e) { this.setData({ regPwd: e.detail.value }); },
  onRegPwd2(e) { this.setData({ regPwd2: e.detail.value }); },

  fail(err) {
    this.setData({ busy: false });
    wx.hideLoading();
    // Toast 约 14 字截断，长消息改用 modal 完整显示
    const msg = (err && err.message) || '操作失败';
    if (msg.length > 12) wx.showModal({ title: '提示', content: msg, showCancel: false });
    else wx.showToast({ title: msg, icon: 'none' });
  },

  afterLogin() {
    this.setData({ busy: false });
    wx.hideLoading();
    wx.showToast({ title: '已登录' });
    wx.switchTab({ url: '/pages/home/home' });
  },

  onWxLogin() {
    if (this.data.busy) return;
    const that = this;
    this.setData({ busy: true });
    wx.showLoading({ title: '登录中', mask: true });
    wx.login({
      success(r) {
        api.post('/api/FtAuth/WeChatLogin', { code: r.code || '' })
          .then(res => { api.saveLogin(res); that.afterLogin(); })
          .catch(err => that.fail(err));
      },
      fail() {
        that.fail(new Error('微信登录失败，请重试'));
      }
    });
  },

  onIdLogin() {
    if (this.data.busy) return;
    const id = (this.data.idCard || '').trim();
    // 登录名可以是手机号/拼音，也可以是身份证；只有看起来像身份证时才验校验位
    const looksLikeId = /^\d{17}[\dXx]$/.test(id);
    const msg = v.firstError([
      { ok: id.length > 0, message: '请输入登录名或身份证号' },
      { ok: !looksLikeId || v.isIdCard(id), message: '身份证号校验位不正确，请核对' },
      { ok: v.isPassword(this.data.password), message: '密码至少 6 位' }
    ]);
    if (msg) { this.fail(new Error(msg)); return; }

    this.setData({ busy: true });
    wx.showLoading({ title: '登录中', mask: true });
    api.post('/api/FtAuth/IdCardLogin', { idCard: id, password: this.data.password })
      .then(res => { api.saveLogin(res); this.afterLogin(); })
      .catch(err => this.fail(err));
  },

  onRegister() {
    if (this.data.busy) return;
    const msg = v.firstError([
      { ok: v.isLoginName(this.data.regId), message: '登录名至少 6 位，只能用字母或数字' },
      { ok: (this.data.realName || '').trim().length > 0, message: '请填写姓名' },
      { ok: v.isPassword(this.data.regPwd), message: '密码至少 6 位' },
      { ok: this.data.regPwd === this.data.regPwd2, message: '两次输入的密码不一致' }
    ]);
    if (msg) { this.fail(new Error(msg)); return; }

    this.setData({ busy: true });
    wx.showLoading({ title: '注册中', mask: true });
    api.post('/api/FtAuth/Register', {
      loginName: this.data.regId,
      realName: this.data.realName,
      password: this.data.regPwd,
      password2: this.data.regPwd2
    })
      .then(res => { api.saveLogin(res); this.afterLogin(); })
      .catch(err => this.fail(err));
  }
});
