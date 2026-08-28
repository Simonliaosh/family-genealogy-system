const api = require('../../utils/request');

function flatten(node, depth, acc) {
  if (!node) return acc;
  acc.push({
    key: (node.id || 0) + '-' + depth + '-' + acc.length,
    name: node.name,
    birth: node.birth,
    depth
  });
  (node.children || []).forEach(c => flatten(c, depth + 1, acc));
  return acc;
}

Page({
  data: {
    mode: 'down',
    scope: 'main',
    roots: [],
    rootNames: [],
    rootIndex: 0,
    lines: [],
    bound: true
  },
  onShow() {
    if (!wx.getStorageSync('ft_token')) {
      wx.reLaunch({ url: '/pages/login/login' });
      return;
    }
    this.loadRoots().then(() => this.loadTree());
  },
  loadRoots() {
    return api.get('/api/FtTree/Roots').then(res => {
      const roots = res.data || [];
      this.setData({ roots, rootNames: roots.map(x => x.name) });
    }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  loadTree() {
    const mode = this.data.mode;
    if (this.data.scope === 'mine') {
      api.get('/api/FtTree/Mine', { mode }).then(res => {
        const d = res.data || {};
        if (d.bound === false) {
          this.setData({ bound: false, lines: [] });
          return;
        }
        this.applyPack(d);
      }).catch(err => wx.showToast({ title: err.message, icon: 'none' }));
      return;
    }
    const root = this.data.roots[this.data.rootIndex];
    const q = { mode };
    if (root) q.rootId = root.id;
    api.get('/api/FtTree/Main', q).then(res => this.applyPack(res.data || {}))
      .catch(err => wx.showToast({ title: err.message, icon: 'none' }));
  },
  applyPack(d) {
    let lines = [];
    if (d.mode === 'side') {
      (d.siblings || []).forEach((s, i) => lines.push({
        key: 's' + i, name: s.name, birth: s.birth, depth: 0
      }));
    } else {
      flatten(d.node, 0, lines);
    }
    this.setData({ bound: true, lines });
  },
  setMode(e) { this.setData({ mode: e.currentTarget.dataset.m }); this.loadTree(); },
  setScope(e) { this.setData({ scope: e.currentTarget.dataset.s }); this.loadTree(); },
  onRoot(e) {
    this.setData({ rootIndex: parseInt(e.detail.value, 10) });
    this.loadTree();
  }
});
