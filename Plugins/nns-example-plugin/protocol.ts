import { protocol, sendMessage } from '../nns-extender/module'
const z = 0x00;



protocol(({ command, adaptor, logger }) => {
    switch (command) {
        /*
        case 0xDF:
            var sessionId = adaptor.Read();
            var frame = readFrame(adaptor, logger);
            break;
        case 0x02:
            receiveMessage(adaptor, logger);
            break;
        */
        default:
            sendMessage(adaptor, logger, [z, z, z, z]);
            break;
       
    }
});