"""Fetch public source pages and preserve evidence outside Unity's Assets tree."""
import argparse
import base64
import hashlib
import json
import sys
from html.parser import HTMLParser
from pathlib import Path
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
sys.stdout.reconfigure(encoding='utf8')

class Page(HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []
        self.text = []
        self.hidden = 0

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag in ('script', 'style'):
            self.hidden += 1
        if tag == 'a' and 'href' in attrs:
            self.links.append(attrs['href'])

    def handle_endtag(self, tag):
        if tag in ('script', 'style'):
            self.hidden = max(0, self.hidden - 1)

    def handle_data(self, data):
        if not self.hidden and data.strip():
            self.text.append(data.strip())

parser = argparse.ArgumentParser()
parser.add_argument('url')
parser.add_argument('--download', help='Project-relative destination for a verified asset')
parser.add_argument('--github-content', action='store_true', help='Decode GitHub contents API base64 response')
args = parser.parse_args()
req = Request(args.url, headers={'User-Agent': 'WenXinJieAssetResearch/1.0'})
with urlopen(req, timeout=35) as response:
    data = response.read()
    content_type = response.headers.get('Content-Type', '')
if args.download:
    if args.github_content:
        payload = json.loads(data)
        if payload.get('encoding') != 'base64':
            raise ValueError('Expected GitHub base64 content')
        data = base64.b64decode(payload['content'])
    target = (ROOT / args.download).resolve()
    if ROOT not in target.parents:
        raise ValueError('Destination must be inside the project')
    if target.exists():
        raise FileExistsError(target)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(data)
    print(json.dumps({'path': str(target), 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}))
else:
    folder = ROOT / 'Docs/AssetResearch'
    folder.mkdir(parents=True, exist_ok=True)
    filename = hashlib.sha256(args.url.encode()).hexdigest()[:16]
    (folder / (filename + '.source')).write_bytes(data)
    (folder / (filename + '.url.txt')).write_text(args.url, encoding='utf8')
    if 'json' in content_type:
        payload = json.loads(data)
        if isinstance(payload, dict) and isinstance(payload.get('data'), list):
            print(json.dumps([{key: item.get(key) for key in ('id', 'title', 'share_license_status', 'url', 'images', 'creators')} for item in payload['data']], ensure_ascii=False))
        else:
            print(data.decode('utf8'))
    else:
        page = Page()
        page.feed(data.decode('utf8', errors='replace'))
        print(json.dumps({'url': args.url, 'text': '\n'.join(page.text), 'links': list(dict.fromkeys(page.links))}, ensure_ascii=False))
