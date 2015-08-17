declare class AmEvent {
    index: number;
}

declare class AmChart {
    addListener (eventName: string, handler: (e: AmEvent) => void) : void;
    zoomToCategoryValues (min: Object, max: Object) : void;
}

declare class AmCharts {
    static makeChart (name: string, options: Object) : AmChart;
}
