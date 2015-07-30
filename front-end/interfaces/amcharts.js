declare class AmEvent {
    index: number;
}

declare class AmChart {
    addListener (eventName: string, handler: (e: AmEvent) => void) : void;
}

declare class AmCharts {
    static makeChart (name: string, options: Object) : AmChart;
}
