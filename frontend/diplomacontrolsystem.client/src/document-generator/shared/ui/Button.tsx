import type { ButtonHTMLAttributes, PropsWithChildren } from 'react'
import { cn } from '../lib/cn'

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger' | 'success' | 'successOutline'
type ButtonSize = 'sm' | 'md' | 'lg' | 'pill'

const variants: Record<ButtonVariant, string> = {
  primary:
    'border border-[var(--color-primary)] bg-[var(--color-primary)] text-white shadow-[var(--shadow-ui)] hover:border-[var(--color-primary-hover)] hover:bg-[var(--color-primary-hover)] active:border-[var(--color-primary)] active:bg-[var(--color-primary)] disabled:border-transparent disabled:bg-white disabled:text-[var(--color-muted)] disabled:opacity-55',
  secondary:
    'border border-[var(--color-primary)] bg-white text-[var(--color-primary)] shadow-[var(--shadow-ui)] hover:bg-[var(--color-bg-lavender)] active:bg-[var(--color-primary)] active:text-white disabled:border-slate-200 disabled:bg-white disabled:text-slate-400',
  ghost:
    'border border-transparent bg-transparent text-[var(--color-muted)] hover:border-[var(--color-primary-hover)] hover:bg-white hover:text-[var(--color-primary)] active:bg-[var(--color-bg-lavender)] disabled:text-slate-300',
  danger:
    'border border-[var(--color-danger)] bg-[var(--color-danger-soft)] text-[var(--color-danger)] hover:bg-white active:bg-[var(--color-danger)] active:text-white disabled:border-slate-200 disabled:bg-white disabled:text-slate-300',
  success:
    'border border-[var(--color-success)] bg-white text-[var(--color-success)] shadow-[var(--shadow-ui)] hover:bg-[#eefdec] active:bg-[var(--color-success)] active:text-white disabled:border-slate-200 disabled:bg-white disabled:text-slate-300',
  successOutline:
    'border border-[var(--color-success)] bg-white text-[var(--color-success)] shadow-[var(--shadow-ui)] hover:bg-[#eefdec] active:bg-[var(--color-success)] active:text-white disabled:border-slate-200 disabled:bg-white disabled:text-slate-300',
}

const sizes: Record<ButtonSize, string> = {
  sm: 'min-h-9 rounded-[var(--radius-ui-pill)] px-5 py-1.5 text-sm',
  md: 'min-h-11 rounded-[var(--radius-ui-sm)] px-5 py-2 text-sm',
  lg: 'min-h-[55px] rounded-[16px] px-7 py-3 text-lg',
  pill: 'min-h-[50px] rounded-[var(--radius-ui-pill)] px-8 py-3 text-lg',
}

type ButtonProps = PropsWithChildren<
  ButtonHTMLAttributes<HTMLButtonElement> & {
    variant?: ButtonVariant
    size?: ButtonSize
  }
>

export function Button({ className, variant = 'primary', size = 'md', children, ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex items-center justify-center gap-2 whitespace-nowrap font-bold leading-none transition focus:outline-none focus:ring-0 focus-visible:shadow-[var(--focus-ring)] disabled:cursor-not-allowed',
        variants[variant],
        sizes[size],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  )
}
