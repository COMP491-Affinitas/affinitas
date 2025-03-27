import json

import tornado.ioloop
import tornado.web
import tornado.websocket


class WebSocket(tornado.websocket.WebSocketHandler):
    clients = set()

    def open(self):
        WebSocket.clients.add(self)
        print("Websocket opened. Client count:", len(WebSocket.clients))

    def on_message(self, message):
        try:
            data = json.loads(message)
            print("Received", data)
        except json.JSONDecodeError:
            print("Received invalid JSON data:", message, sep="\n")
            self.write_message(json.dumps({"error": "Invalid JSON"}))
            return

        self.write_message(json.dumps({
            "message": "message received"
        }))

    def on_close(self):
        self.clients.remove(self)
        print("WebSocket closed. Total clients: ", len(self.clients))

    def check_origin(self, origin):
        return True

if __name__ == "__main__":
    app = tornado.web.Application([("/websocket", WebSocket)])
    app.listen(8888, address="0.0.0.0")
    try:
        print("Listening on ws://127.0.0.1:8888/websocket")
        tornado.ioloop.IOLoop.current().start()
    except KeyboardInterrupt:
        print("shutting down")
        tornado.ioloop.IOLoop.current().stop()