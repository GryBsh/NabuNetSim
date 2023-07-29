import { Adaptor, AdaptorFrame, Logger, protocol } from '../nns-extender/module'
const z = 0x00;

function receiveMessage(adaptor: Adaptor, logger: Logger) {
    logger.Write("Received Message");

    const buffer = new Uint8Array([z, z, z, z]);
    adaptor.Write(buffer);
}

function readFrame(adaptor: Adaptor, logger: Logger): AdaptorFrame {
    const length = adaptor.ReadShort();
    const frame = adaptor.Read(length);
    logger.Write(`Received Frame: ${length} bytes`);
    return {
        Length: length,
        Data: frame
    };
}

protocol(({ command, adaptor, logger }) => {
    switch (command) {
        case 0xDF:
            readFrame(adaptor, logger);
            break;
        case 0x02:
            receiveMessage(adaptor, logger);
            break;
    }
});