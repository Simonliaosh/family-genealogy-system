const api = require('../../utils/request');
const page = require('../../utils/page');
const v = require('../../utils/validate');

Page({
  data: {
    bound: true,
    personId: 0,
    fullName: '',
    nickName: '',
    selfIntro: '',
    wechatId: '',
    showPhoto: false,
    showWechat: false,
    printAllow: true,
    photos: [],
    bindId: '',
    loading: false,
    loadError: ''
  },
  onShow() {
    if (!page.requireLogin()) return;
    this.load();
  },
  onPullDownRefresh() { this.load({ silent: true }); },
  onRetry() { this.load(); },
  load(opts) {
    return page.load(this, () => api.get('/api/FtProfile').then(res => {
      const d = res.data || {};
      if (!d.bound) {
        this.setData({ bound: false });
        return;
      }
      this.setData({
        bound: true,
        personId: d.personId,
        fullName: d.fullName || '',
        nickName: d.nickName || '',
        selfIntro: d.selfIntro || '',
        wechatId: d.wechatId || '',
        showPhoto: !!d.showPhoto,
        showWechat: !!d.showWechat,
        printAllow: d.printAllow !== false,
        photos: (d.photos || []).map(api.absUrl)
      });
    }), opts).catch(() => { /* 错误已写进 loadError */ });
  },
  set(e) { this.setData({ [e.currentTarget.dataset.k]: e.detail.value }); },
  onSwitch(e) { this.setData({ [e.currentTarget.dataset.k]: e.detail.value }); },
  save() {
    api.post('/api/FtProfile', {
      personId: this.data.personId,
      nickName: this.data.nickName,
      selfIntro: this.data.selfIntro,
      wechatId: this.data.wechatId,
      showPhoto: this.data.showPhoto,
      showWechat: this.data.showWechat,
      printAllow: this.data.printAllow
    }).then(res => {
      wx.showToast({ title: res.message || '已保存' });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  choosePhoto() {
    const that = this;
    // wx.chooseImage 自基础库 2.21.0 起已废弃，本项目锁的是 3.5.5，直接用 wx.chooseMedia。
    wx.chooseMedia({
      count: 1,
      mediaType: ['image'],
      sizeType: ['compressed'],
      success(r) {
        const file = (r.tempFiles || [])[0];
        if (!file || !file.tempFilePath) return;
        wx.showLoading({ title: '上传中', mask: true });
        api.uploadPhoto(file.tempFilePath, that.data.personId).then(() => {
          wx.hideLoading();
          wx.showToast({ title: '已上传' });
          that.load();
        }).catch(err => {
          wx.hideLoading();
          wx.showToast({ title: err.message, icon: 'none' });
        });
      },
      // 原先没有 fail：相册权限被拒是完全静默的，用户点了没反应也不知道为什么
      fail(err) {
        const msg = (err && err.errMsg) || '';
        if (/cancel/i.test(msg)) return;          // 用户主动取消，不打扰
        if (/auth|permission/i.test(msg)) {
          wx.showModal({
            title: '需要相册权限',
            content: '请在「设置」里允许访问相册后重试。',
            confirmText: '去设置',
            success(r2) { if (r2.confirm) wx.openSetting({}); }
          });
          return;
        }
        wx.showToast({ title: '选择图片失败', icon: 'none' });
      }
    });
  },
  delPhoto(e) {
    const path = e.currentTarget.dataset.path || '';
    const rel = path.replace(/^https?:\/\/[^/]+/i, '');
    wx.showModal({
      title: '删除这张照片？',
      success: (r) => {
        if (!r.confirm) return;
        api.post('/api/FtProfile/DeletePhoto', { personId: this.data.personId, path: rel })
          .then(() => this.load())
          .catch(err => wx.showToast({ title: err.message, icon: 'none' }));
      }
    });
  },
  bindIdCard() {
    if (!v.isIdCard(this.data.bindId)) {
      wx.showModal({
        title: '身份证号有误',
        content: '请输入 18 位身份证号（末位可为 X），并核对校验位。',
        showCancel: false
      });
      return;
    }
    api.post('/api/FtAuth/BindIdCard', { idCard: this.data.bindId })
      .then(res => wx.showToast({ title: res.message || '已绑定' }))
      .catch(err => wx.showModal({ title: '绑定失败', content: err.message, showCancel: false }));
  }
});
