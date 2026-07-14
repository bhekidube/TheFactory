
export default {
  bootstrap: () => import('./main.server.mjs').then(m => m.default),
  inlineCriticalCss: true,
  baseHref: '/',
  locale: undefined,
  routes: [
  {
    "renderMode": 2,
    "route": "/"
  }
],
  entryPointToBrowserMapping: undefined,
  assets: {
    'index.csr.html': {size: 545, hash: 'bd12fd0c6f1760f2c9c1357a94be7ca62b98cd16067bbb631e7b53801bfbda4d', text: () => import('./assets-chunks/index_csr_html.mjs').then(m => m.default)},
    'index.server.html': {size: 947, hash: '5bddd021d570900ee4dac8b902604db2c1d9d69be6f7a1e3cc4d985e936c6a3c', text: () => import('./assets-chunks/index_server_html.mjs').then(m => m.default)},
    'index.html': {size: 1567, hash: '7b04e1bfb9fa48eac2d242a347d79d2707cf1c638f4d18e5ee9b402191101d1d', text: () => import('./assets-chunks/index_html.mjs').then(m => m.default)},
    'styles-BUKGVV7J.css': {size: 552, hash: 'qlQT2sMl13s', text: () => import('./assets-chunks/styles-BUKGVV7J_css.mjs').then(m => m.default)}
  },
};
