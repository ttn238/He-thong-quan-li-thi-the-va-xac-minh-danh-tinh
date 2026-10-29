// Các chuỗi class Tailwind dùng lại nhiều lần trong thư mục ai-result
export const S = {
    card: 'rounded-md border border-line bg-white p-4',
    cardWarn: 'rounded-md border border-amber-300 bg-amber-50 p-4',
    head: 'mb-2.5 flex items-center justify-between gap-2',
    title: 'm-0 text-[15px] font-semibold text-navy',
    muted: 'my-1 text-[13px] text-muted',
    tag: 'ml-2 inline-block rounded-full bg-blue-100 px-2 py-px text-xs text-navy',
    tagWarn: 'ml-2 inline-block rounded-full bg-amber-100 px-2 py-px text-xs text-amber-800',
    btn: 'cursor-pointer rounded border-0 bg-navy px-4 py-2 font-semibold text-white disabled:cursor-not-allowed disabled:opacity-60',
    btnGhost: 'cursor-pointer rounded border border-navy bg-white px-4 py-2 font-semibold text-navy',
    link: 'cursor-pointer border-0 bg-transparent p-0 text-[13px] text-brand hover:underline',
    item: 'relative rounded-md border border-line bg-soft px-3 py-2.5 text-sm',
    itemLow: 'relative rounded-md border border-amber-300 bg-amber-50 px-3 py-2.5 text-sm',
    kv: 'm-0 grid grid-cols-[150px_1fr] gap-x-3 gap-y-1.5 text-sm [&>dt]:text-muted [&>dd]:m-0 [&>dd]:break-words',
    state: 'rounded-md border border-line bg-white p-6 text-center',
} as const;