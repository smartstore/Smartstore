const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const root = path.resolve(__dirname, '../../..');
const source = fs.readFileSync(path.join(root, 'src/Smartstore.Web/wwwroot/lib/roxyfm/js/main.js'), 'utf8');

function extractFunction(name) {
    const start = source.indexOf('function ' + name + '(');
    const end = source.indexOf('\nfunction ', start + 1);
    return source.slice(start, end < 0 ? undefined : end);
}

test('Roxy AJAX sends tokens on same-origin POST only', () => {
    let prefilter;
    const $ = () => ({ val: () => 'test-token' });
    $.ajaxSetup = () => {};
    $.ajaxPrefilter = fn => { prefilter = fn; };
    vm.runInNewContext(source.slice(0, source.indexOf('function selectFile(')), { $ });

    for (const [type, crossDomain, expected] of [
        ['POST', false, true], ['post', false, true], ['GET', false, false], ['POST', true, false]
    ]) {
        const headers = {};
        prefilter({ type, crossDomain }, {}, { setRequestHeader: (name, value) => { headers[name] = value; } });
        assert.deepEqual(headers, expected ? { 'X-XSRF-Token': 'test-token' } : {});
    }
});

test('Direct file upload includes the form token and keeps the existing payload', () => {
    let request;
    const context = {
        $: () => ({ val: () => 'test-token', attr: () => 'file' }),
        findUploadElement: () => ({ find: () => ({ remove() {} }) }),
        FormData: class {
            constructor() { this.fields = new Map(); }
            append(name, value) { this.fields.set(name, value); }
        },
        XMLHttpRequest: class {
            constructor() { request = this; this.upload = { addEventListener() {} }; }
            addEventListener() {}
            open(method, url) { this.method = method; this.url = url; }
            setRequestHeader() {}
            send(body) { this.body = body; }
        },
        RoxyUtils: { GetRootPath: value => value },
        RoxyFilemanConf: { UPLOAD: '/admin/roxyfilemanager/processrequest?a=UPLOAD' }
    };
    vm.createContext(context);
    vm.runInContext(extractFunction('fileUpload'), context);
    const file = {};
    context.fileUpload(file, 0);
    assert.equal(request.method, 'POST');
    assert.equal(request.body.fields.get('__RequestVerificationToken'), 'test-token');
    assert.equal(request.body.fields.get('files[]'), file);
    assert.equal(request.body.fields.get('method'), 'ajax');
    assert.equal(request.body.fields.get('d'), 'file');
});

test('File and directory downloads retain iframe navigation', () => {
    const frame = { location: {} };
    const context = {
        window: { frames: { frmUploadFile: frame } },
        getSelectedFile: () => ({ fullPath: 'file/photo.jpg' }),
        getSelectedDir: () => ({ fullPath: 'file/photos' }),
        RoxyFilemanConf: { DOWNLOAD: '/download', DOWNLOADDIR: '/download-dir' },
        RoxyUtils: {
            GetRootPath: value => value,
            AddParam: (url, name, value) => url + '?' + name + '=' + encodeURIComponent(value)
        }
    };
    vm.createContext(context);
    vm.runInContext(extractFunction('downloadFile') + extractFunction('downloadDir'), context);
    context.downloadFile();
    assert.equal(frame.location.href, '/download?f=file%2Fphoto.jpg');
    context.downloadDir();
    assert.equal(frame.location.href, '/download-dir?d=file%2Fphotos');
});
