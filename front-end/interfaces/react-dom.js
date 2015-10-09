declare module "react-dom" {
    declare function render<DefaultProps, Props, State>(
        element: ReactElement<DefaultProps, Props, State>,
        container: any
    ): ReactComponent<DefaultProps, Props, State>;
}
