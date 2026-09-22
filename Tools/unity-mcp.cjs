const { spawn } = require('node:child_process');
const path = require('node:path');
const fs = require('node:fs');
const os = require('node:os');
const readline = require('node:readline');

const root = path.resolve(__dirname, '..');
const relay = path.join(root, 'Library/PackageCache/com.unity.ai.assistant@8d1e3e89e3a7/RelayApp~/relay_win.exe');
const connections = path.join(os.homedir(), '.unity/mcp/connections');
const bridges = fs.readdirSync(connections).filter(name => name.endsWith('.json')).map(name => {
  const file = path.join(connections, name);
  return { ...JSON.parse(fs.readFileSync(file, 'utf8')), modified: fs.statSync(file).mtimeMs };
}).filter(bridge => {
  if (path.resolve(bridge.project_path).toLowerCase() !== root.toLowerCase()) return false;
  try { process.kill(bridge.editor_pid, 0); return true; } catch { return false; }
})
  .sort((a, b) => b.modified - a.modified);
if (!bridges.length) {
  console.error('No live project bridge. Unity may be compiling or reloading; retry after it finishes.');
  process.exit(1);
}
const relayArgs = ['--mcp', '--project-path', root];
if (bridges.length) relayArgs.push('--instance-id', String(bridges[0].editor_pid));
const child = spawn(relay, relayArgs, { windowsHide: true });
let sequence = 0;
const pending = new Map();
const timeout = setTimeout(() => { console.error('Unity MCP timed out. Check editor compilation, connection approval, or the requested cloud service.'); child.kill(); process.exitCode = 1; }, process.argv[2] === 'Unity_AssetGeneration_GetModels' ? 180000 : 55000);
child.stderr.on('data', data => { if (/\[ERROR\]|approval.*pending/i.test(data.toString())) process.stderr.write(data); });
child.on('error', error => { console.error(error.message); clearTimeout(timeout); process.exitCode = 1; });
readline.createInterface({ input: child.stdout }).on('line', line => {
  let response;
  try { response = JSON.parse(line); } catch { return; }
  if (pending.has(response.id)) { pending.get(response.id)(response); pending.delete(response.id); }
});
function request(method, params) {
  return new Promise(resolve => {
    const id = ++sequence;
    pending.set(id, resolve);
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n');
  });
}
(async () => {
  try {
    const init = await request('initialize', { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'xianxia-codex', version: '1.0.0' } });
    if (init.error) throw new Error(JSON.stringify(init.error));
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) + '\n');
    const name = process.argv[2];
    const input = process.argv[3] ? fs.readFileSync(process.argv[3], 'utf8').replace(/^\uFEFF/, '') : '';
    const args = process.argv[3]?.endsWith('.cs') ? { Code: input, Title: path.basename(process.argv[3]) } : input ? JSON.parse(input) : {};
    const result = await request(name ? 'tools/call' : 'tools/list', name ? { name, arguments: args } : {});
    const output = result.result?.structuredContent || result;
    if (output.data) delete output.data.localFixedCode;
    console.log(JSON.stringify(output, null, 2));
    if (result.error || result.result?.isError) process.exitCode = 1;
  } catch (error) { console.error(error.message); process.exitCode = 1; }
  finally { clearTimeout(timeout); child.kill(); }
})();
