
logger.Write("Got: "+ global.incoming);
global.adaptor.Send(0x10, 0x06);
logger.Write("Sent 0x10|0x06");