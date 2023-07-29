# nns-extender

## Protocol

```ts
protocol(({command, adaptor, logger}) => {
    switch (command) {
        case 0x02:
            logger.Write("Received Message")
            break;
    }
});
```