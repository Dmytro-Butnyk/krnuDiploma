import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'
import { cn } from '../lib/cn'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'success'

const variants: Record<ButtonVariant, string> = {
  primary:
    'border border-blue-700 bg-blue-600 text-white shadow-sm hover:bg-blue-500 active:bg-blue-700 disabled:border-blue-200 disabled:bg-blue-100 disabled:text-blue-300',
  secondary:
    'border border-blue-300 bg-white text-blue-700 shadow-sm hover:bg-blue-50 active:bg-blue-100 disabled:border-slate-200 disabled:text-slate-400 disabled:hover:bg-white',
  ghost:
    'border border-transparent text-slate-600 hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700 active:bg-blue-100 disabled:text-slate-300',
  danger:
    'border border-red-300 bg-red-50 text-red-600 hover:border-red-400 hover:bg-red-100 active:bg-red-200 disabled:text-red-300',
  success:
    'border border-lime-500 bg-white text-lime-600 shadow-sm hover:bg-blue-50 hover:text-blue-700 active:bg-blue-100 disabled:border-slate-200 disabled:text-slate-300',
}

type ButtonProps = PropsWithChildren<
  ButtonHTMLAttributes<HTMLButtonElement> & {
    variant?: ButtonVariant
  }
>

export function Button({ className, variant = 'primary', children, ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex min-h-10 items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-semibold transition focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed',
        variants[variant],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  )
}
