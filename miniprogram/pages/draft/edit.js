const api = require('../../utils/request');
const rels = [
  { value: 'SELF', text: '本人' },
  { value: 'FATHER', text: '父亲' },
  { value: 'MOTHER', text: '母亲' },
  { value: 'CHILD', text: '子女' },
  { value: 'SIBLING', text: '兄弟姐妹' }
];

Page({
  data: {
    dataId: 0,
    fullName: '',
    fatherName: '',
    motherName: '',
    birthDate: '',
    relationType: 'SELF',
    gender: 1,
    generationNo: 0,
    wordOfGeneration: '',
    remark: '',
    relIndex: 0,
    relLabels: rels.map(x => x.text),
    genders: ['女', '男']
  },
  onLoad(q) {
    if (q.id) this.load(parseInt(q.id, 10));
  },
  load(id) {
    api.get('/api/FtDraft/' + id).then(res => {
      const d = res.data || {};
      const idx = Math.max(0, rels.findIndex(x => x.value === d.relationType));
      this.setData({
        dataId: d.dataId || id,
        fullName: d.fullName || '',
        fatherName: d.fatherName || '',
        motherName: d.motherName || '',
        birthDate: d.birthDate || '',
        relationType: d.relationType || 'SELF',
        gender: d.gender == 0 ? 0 : 1,
        generationNo: d.generationNo || 0,
        wordOfGeneration: d.wordOfGeneration || '',
        remark: d.remark || '',
        relIndex: idx
      });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  set(e) {
    const k = e.currentTarget.dataset.k;
    this.setData({ [k]: e.detail.value });
  },
  onRel(e) {
    const i = parseInt(e.detail.value, 10);
    this.setData({ relIndex: i, relationType: rels[i].value });
  },
  onGender(e) {
    this.setData({ gender: parseInt(e.detail.value, 10) });
  },
  payload() {
    return {
      dataId: this.data.dataId,
      fullName: this.data.fullName,
      fatherName: this.data.fatherName,
      motherName: this.data.motherName,
      birthDate: this.data.birthDate,
      relationType: this.data.relationType,
      gender: this.data.gender,
      generationNo: parseInt(this.data.generationNo, 10) || 0,
      wordOfGeneration: this.data.wordOfGeneration,
      remark: this.data.remark
    };
  },
  save() {
    api.post('/api/FtDraft', this.payload()).then(res => {
      const id = (res.data && res.data.id) || this.data.dataId;
      this.setData({ dataId: id });
      wx.showToast({ title: res.message || '已保存' });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  complete() {
    const id = this.data.dataId;
    if (!id) {
      wx.showToast({ title: '请先保存草稿', icon: 'none' });
      return;
    }
    api.post('/api/FtDraft', this.payload()).then(() =>
      api.post('/api/FtDraft/' + id + '/Complete', {})
    ).then(res => {
      wx.showModal({
        title: '已落档',
        content: res.message || ('人物 #' + ((res.data && res.data.personId) || '')),
        showCancel: false,
        success() { wx.navigateBack(); }
      });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  }
});
