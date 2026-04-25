import asyncio,socket
import websockets
HOST='127.0.0.1';UDP_IN=5005;WS_PORT=8080;CMD_OUT=5006;clients=set();cmd_queue=asyncio.Queue()
async def ws_handler(ws):
 clients.add(ws)
 try:
  async for msg in ws:
   await cmd_queue.put(msg.encode())
 except Exception:
  pass
 finally:
  clients.discard(ws)
async def telemetry_loop(udp_in):
 loop=asyncio.get_running_loop()
 while 1:
  try:data,_=await asyncio.wait_for(loop.sock_recvfrom(udp_in,65535),.001)
  except asyncio.TimeoutError:continue
  except Exception:await asyncio.sleep(.001);continue
  if not data or not clients:continue
  text=data.decode('utf-8','ignore');drop=[]
  for ws in clients:
   try:await ws.send(text)
   except Exception:drop.append(ws)
  for ws in drop:clients.discard(ws)
async def command_loop(udp_out):
 while 1:
  try:udp_out.sendto(await cmd_queue.get(),(HOST,CMD_OUT))
  except Exception:await asyncio.sleep(.001)
async def main():
 udp_in=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);udp_in.bind((HOST,UDP_IN));udp_in.setblocking(False)
 udp_out=socket.socket(socket.AF_INET,socket.SOCK_DGRAM);udp_out.setblocking(False)
 async with websockets.serve(ws_handler,HOST,WS_PORT):
  await asyncio.gather(telemetry_loop(udp_in),command_loop(udp_out))
if __name__=='__main__':
 asyncio.run(main())
