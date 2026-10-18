"""Loopback-only, read-only audition server. Only catalog media and page files are served."""
import argparse
import json
import mimetypes
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
PREFIX = '/' + HERE.relative_to(ROOT).as_posix() + '/'
CATALOG = json.loads((HERE / 'catalog.json').read_text(encoding='utf-8'))
ALLOWED = {'/' + c['path'] for g in CATALOG['groups'] for c in g['clips']}
ALLOWED.update(PREFIX + name for name in ('index.html', 'style.css', 'app.js', 'catalog.js', 'catalog.json'))

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        self.respond(False)

    def do_HEAD(self):
        self.respond(True)

    def respond(self, head):
        route = unquote(urlsplit(self.path).path)
        if route == '/':
            self.send_response(302)
            self.send_header('Location', PREFIX + 'index.html')
            self.end_headers()
            return
        if route not in ALLOWED:
            self.send_error(404)
            return
        path = (ROOT / route.lstrip('/')).resolve()
        if not path.is_relative_to(ROOT) or not path.is_file():
            self.send_error(404)
            return
        size = path.stat().st_size
        start, end, status = 0, size - 1, 200
        requested = self.headers.get('Range')
        if requested:
            try:
                unit, bounds = requested.split('=', 1)
                low, high = bounds.split('-', 1)
                if unit != 'bytes' or ',' in bounds:
                    raise ValueError()
                if low:
                    start = int(low)
                    end = min(int(high), end) if high else end
                else:
                    start = max(0, size - int(high))
                if start < 0 or start > end:
                    raise ValueError()
                status = 206
            except ValueError:
                self.send_error(416)
                return
        self.send_response(status)
        self.send_header('Content-Type', mimetypes.guess_type(path)[0] or 'application/octet-stream')
        self.send_header('Content-Length', str(end - start + 1))
        self.send_header('Accept-Ranges', 'bytes')
        self.send_header('Cache-Control', 'no-cache')
        self.send_header('X-Content-Type-Options', 'nosniff')
        if status == 206:
            self.send_header('Content-Range', f'bytes {start}-{end}/{size}')
        self.end_headers()
        if not head:
            with path.open('rb') as f:
                f.seek(start)
                remaining = end - start + 1
                try:
                    while remaining:
                        data = f.read(min(65536, remaining))
                        if not data:
                            break
                        self.wfile.write(data)
                        remaining -= len(data)
                except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                    pass

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=8879)
    args = parser.parse_args()
    print(f'http://127.0.0.1:{args.port}{PREFIX}index.html', flush=True)
    ThreadingHTTPServer(('127.0.0.1', args.port), Handler).serve_forever()
