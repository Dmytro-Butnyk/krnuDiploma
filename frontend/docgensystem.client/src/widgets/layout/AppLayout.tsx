import { LogOut, UserRound } from 'lucide-react'
import { Outlet } from 'react-router-dom'

export function AppLayout() {
  return (
    <div className="min-h-screen bg-[#dfeff6] text-slate-950">
      <header className="mx-auto flex w-full max-w-[1120px] items-center justify-between px-7 pt-7">
        <div className="h-12 w-12 rounded-xl bg-slate-300/70" />
        <div className="flex items-center gap-4">
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-blue-600 text-white shadow-sm ring-4 ring-white"
            title="Профіль"
          >
            <UserRound size={22} />
          </button>
          <button
            className="flex h-11 w-11 items-center justify-center rounded-full bg-red-100 text-red-500"
            title="Вийти"
          >
            <LogOut size={20} />
          </button>
        </div>
      </header>

      <main className="mx-auto w-full max-w-[1120px] px-7 pb-10 pt-8">
        <Outlet />
      </main>
    </div>
  )
}
