"""Read-only loopback server restricted to this audition page and catalogue media."""
import json,mimetypes
from pathlib import Path
from urllib.parse import urlsplit,unquote
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
HERE=Path(__file__).resolve().parent
CAT=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'))
ALLOWED={'/'+n for n in ['index.html','style.css','app.js','catalog.js','catalog.json']}
for g in CAT['groups']:
 for c in g['clips']:
  ALLOWED.add('/'+c['url'])
  if c.get('repeat_url'):ALLOWED.add('/'+c['repeat_url'])
ALLOWED.update('/'+c['url'] for c in CAT['retained'])
ALLOWED.update('/'+p for p in CAT['multi']['clips'])
ALLOWED.update('/'+p for p in CAT['flows'])
ALLOWED.update('/'+c['url'] for c in CAT['tentative']+CAT['extras'])
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):self.respond(False)
 def do_HEAD(self):self.respond(True)
 def respond(self,head):
  route=unquote(urlsplit(self.path).path)
  if route=='/':route='/index.html'
  if route not in ALLOWED:self.send_error(404);return
  p=(HERE/route.lstrip('/')).resolve()
  if not p.is_relative_to(HERE) or not p.is_file():self.send_error(404);return
  total=p.stat().st_size;start=0;end=total-1;status=200
  if self.headers.get('Range'):
   try:
    unit,bounds=self.headers['Range'].split('=',1);low,high=bounds.split('-',1)
    if unit!='bytes' or ',' in bounds:raise ValueError()
    if low:start=int(low);end=min(int(high),end) if high else end
    else:start=max(0,total-int(high))
    if start<0 or start>end:raise ValueError()
    status=206
   except ValueError:self.send_error(416);return
  self.send_response(status);mime=mimetypes.guess_type(p.name)[0] or 'application/octet-stream'
  self.send_header('Content-Type',mime+'; charset=utf-8' if p.suffix in ['.html','.js','.css','.json'] else mime)
  self.send_header('Content-Length',str(end-start+1));self.send_header('Accept-Ranges','bytes')
  if status==206:self.send_header('Content-Range',f'bytes {start}-{end}/{total}')
  self.end_headers()
  if head:return
  try:
   with p.open('rb') as f:
    f.seek(start);remaining=end-start+1
    while remaining:
     data=f.read(min(65536,remaining))
     if not data:break
     self.wfile.write(data);remaining-=len(data)
  except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
if __name__=='__main__':
 print('http://127.0.0.1:8885/',flush=True)
 ThreadingHTTPServer(('127.0.0.1',8885),Handler).serve_forever()
