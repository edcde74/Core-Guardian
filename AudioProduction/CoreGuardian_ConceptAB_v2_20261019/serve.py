"""Local read-only, page-only audition server. No uploads or directory listing."""
from pathlib import Path
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
HERE=Path(__file__).resolve().parent
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):
  if self.path.split('?')[0] not in ['/','/index.html']:
   self.send_error(404);return
  data=(HERE/'index.html').read_bytes()
  self.send_response(200);self.send_header('Content-Type','text/html; charset=utf-8');self.send_header('Content-Length',str(len(data)));self.end_headers()
  try:self.wfile.write(data)
  except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):pass
if __name__=='__main__':
 print('http://127.0.0.1:8881/',flush=True)
 ThreadingHTTPServer(('127.0.0.1',8881),Handler).serve_forever()
