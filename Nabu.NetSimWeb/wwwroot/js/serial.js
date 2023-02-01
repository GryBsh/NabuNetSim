var Serial;
var BrowserSerialTextEncoder = new TextEncoder();

function BrowserSerialIsSupported() {
    return navigator.serial ? true : false;
}

async function BrowserSerialGetPort() {
    try {
        Serial = await navigator.serial.requestPort();
        return "Ok";
    }
    catch (ex) {
        if (ex instanceof SecurityError) {
            return "SecurityError";
        }
        else if (ex instanceof AbortError) {
            return "AbortError";
        }
        else {
            return "Unknown";
        }
    }
}

async function BrowserSerialOpen(baudRate) {
    try {
        await Serial.open({ baudRate: baudRate });
        return "Ok";
    }
    catch (ex) {
        if (ex instanceof InvalidStateError) {
            return "InvalidStateError";
        }
        else if (ex instanceof NetworkError) {
            return "NetworkError";
        }
        else {
            return "Unknown";
        }
    }
}

async function BrowserSerialRead(length) {
    let offset = 0;
    var buffer = new ArrayBuffer(length);
    let reader = Serial.readable.getReader({ mode: "byob" });
    while (offset < buffer.byteLength) {
        const { value, done } = await reader.read(
            new Uint8Array(buffer, offset)
        );
        if (done) {
            break;
        }
        buffer = value.buffer;
        offset += value.byteLength;
    }
    return buffer;
}

function BrowserSerialWrite(buffer) {
    let writer = Serial.writable.getWriter();
    writer.write(buffer);
    writer.releaseLock();
}

function BrowserSerialClose() {
    Serial.close();
}