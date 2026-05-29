import { LogOut, UserRound } from 'lucide-react'
import { Outlet } from 'react-router-dom'

export function AppLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-[var(--color-app-bg)] text-[var(--color-text)]">
      <header className="mx-auto flex w-[min(1680px,calc(100vw-40px))] shrink-0 items-center justify-between pt-[clamp(20px,3vh,38px)]">
        <div className="h-12 w-12 rounded-[16px] bg-white/80 shadow-[var(--shadow-ui)] ring-1 ring-white/80" />
        <div className="flex items-center gap-4">
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-[var(--color-primary)] text-white shadow-[var(--shadow-ui)] ring-4 ring-white transition hover:bg-[var(--color-primary-hover)]"
            title="Профіль"
          >
            <UserRound size={22} />
          </button>
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-[var(--color-danger-soft)] text-[var(--color-danger)] transition hover:bg-white"
            title="Вийти"
          >
            <LogOut size={20} />
          </button>
        </div>
      </header>

      <main className="mx-auto w-[min(1680px,calc(100vw-40px))] flex-1 pb-[clamp(20px,4vh,44px)] pt-[clamp(20px,3vh,36px)]">
        <Outlet />
      </main>
    </div>
  )
}
