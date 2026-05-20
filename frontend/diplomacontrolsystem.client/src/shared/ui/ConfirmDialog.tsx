import type { ReactNode } from 'react'

interface ConfirmDialogProps {
  title: string
  children: ReactNode
  confirmLabel: string
  cancelLabel?: string
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  title,
  children,
  confirmLabel,
  cancelLabel = 'Скасувати',
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#91a6f2]/65 px-4 backdrop-blur-md">
      <section className="w-full max-w-[560px] rounded-[28px] bg-slate-50 px-10 py-12 text-center shadow-2xl">
        <h2 className="text-4xl font-bold text-red-500">{title}</h2>
        <div className="mt-9 text-2xl leading-snug text-slate-500">{children}</div>
        <div className="mt-14 space-y-4">
          <button
            type="button"
            onClick={onConfirm}
            className="h-16 w-full rounded-full bg-red-500 text-2xl font-bold text-white transition hover:bg-red-600 focus:outline-none focus:ring-4 focus:ring-red-200"
          >
            {confirmLabel}
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="h-16 w-full rounded-full bg-white text-2xl font-bold text-blue-300 transition hover:text-blue-500 focus:outline-none focus:ring-4 focus:ring-blue-100"
          >
            {cancelLabel}
          </button>
        </div>
      </section>
    </div>
  )
}
