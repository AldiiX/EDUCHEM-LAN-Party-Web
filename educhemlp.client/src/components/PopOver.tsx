export const PopOver = ({ children, className }: { children: React.ReactNode, className?: string }) => {
    return (
        <div className={"popover"}>
            {children}
        </div>
    );
}