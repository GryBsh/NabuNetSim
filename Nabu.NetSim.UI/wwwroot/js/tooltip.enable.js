const tooltipOptions = {
    trigger: "hover",
    delay: {
        show: 750,
        hide: 0
    }
};

function enableTooltips() {
    var tooltipElements = document.querySelectorAll('[data-bs-toggle="tooltip"]');

    [].slice.call(tooltipElements).forEach((e) => {
        new bootstrap.Tooltip(e, tooltipOptions);        
    });
}

setTimeout(enableTooltips, 4000);