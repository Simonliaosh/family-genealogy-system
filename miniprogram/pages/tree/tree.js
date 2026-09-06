const api = require('../../utils/request');
const page = require('../../utils/page');

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
  data: Object.assign({
    mode: 'down',
    scope: 'main',
    roots: [],
    rootNames: [],
    rootIndex: 0,
    lines: [],
    bound: true
  }, page.loadState),

  onShow() {
    if (!page.requireLogin()) return;
    this.reload();
  },

  onPullDownRefresh() { this.reload({ silent: true }); },

  reload(opts) {
    return page.load(this, () =>
      this.loadRoots().then(() => this.loadTree()), opts).catch(() => {});
  },

  onRetry() { this.reload(); },

  loadRoots() {
    return api.get('/api/FtTree/Roots').then(res => {
      const roots = res.data || [];
      this.setData({ roots, rootNames: roots.map(x => x.name) });
    });
  },
  loadTree() {
    const mode = this.data.mode;
    if (this.data.scope === 'mine') {
      return api.get('/api/FtTree/Mine', { mode }).then(res => {
        const d = res.data || {};
        if (d.bound === false) {
          this.setData({ bound: false, lines: [] });
          return;
        }
        this.applyPack(d);
      });
    }
    const root = this.data.roots[this.data.rootIndex];
    const q = { mode };
    if (root) q.rootId = root.id;
    return api.get('/api/FtTree/Main', q).then(res => this.applyPack(res.data || {}));
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
  setMode(e) {
    this.setData({ mode: e.currentTarget.dataset.m });
    page.load(this, () => this.loadTree()).catch(() => {});
  },
  setScope(e) {
    this.setData({ scope: e.currentTarget.dataset.s });
    page.load(this, () => this.loadTree()).catch(() => {});
  },
  onRoot(e) {
    this.setData({ rootIndex: parseInt(e.detail.value, 10) });
    page.load(this, () => this.loadTree()).catch(() => {});
  }
});
