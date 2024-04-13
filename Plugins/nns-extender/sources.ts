

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