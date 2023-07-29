export declare const _host_command: any;
export declare const _host_adaptor: any;
export declare const _host_logger: any;

export type AdaptorFrame = {
    Length: number
    Data: Uint8Array
};

export type Adaptor = {
    Read(): number
    Read(length: number): Uint8Array
    Write(bytes: Uint8Array): void

    ReadInt(): number
    ReadShort(): number

    //ReadFrame(): AdaptorFrame
    //WriteFrame(buffer: Uint8Array): void
    //WriteFrame(header: number, buffer: Uint8Array): void
};

export type Logger = {
    Write(message: string): void
    WriteVerbose(message: string): void
    WriteError(message: string): void
}

export type ProtocolHandlerArgs = {
    command: number,
    adaptor: Adaptor,
    logger: Logger
}

export type ProtocolHandler = ({ command, adaptor, logger }: ProtocolHandlerArgs) => void;

export function protocol(handler: ProtocolHandler) {
    if (typeof (handler) !== typeof (Function))
        return;
    handler({
        command: _host_command,
        adaptor: _host_adaptor,
        logger: _host_logger
    });
}


export declare const _source_uri : any;

export type NabuProgram = {
    Name: string,
    DisplayName: string,
    Source: string,
    Path: string,
    SourceType: string,
    ImageType: string
};

export type SourceHandler = (uri: string) => NabuProgram[];

export function source(handler: SourceHandler) {
    if (typeof (handler) !== typeof (Function))
        return [];
    return handler(_source_uri);
}