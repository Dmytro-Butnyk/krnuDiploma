import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Navigate, useNavigate } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from '../../features/auth/model/useAuth'

const loginSchema = z.object({
  secretaryEmail: z.email('Введіть коректну електронну пошту'),
})

type LoginFormValues = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const { isAuthenticated, login } = useAuth()
  const {
    formState: { errors },
    handleSubmit,
    register,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      secretaryEmail: '',
    },
  })

  if (isAuthenticated) {
    return <Navigate to="/groups" replace />
  }

  const submit = (values: LoginFormValues) => {
    login(values.secretaryEmail)
    navigate('/groups', { replace: true })
  }

  return (
    <main className="grid min-h-screen place-items-center bg-[radial-gradient(circle_at_20%_10%,#d6f6ec_0,#eaf7f4_28%,transparent_46%),radial-gradient(circle_at_72%_70%,#f2f8da_0,transparent_35%),#ecf7fb] px-6">
      <section className="w-full max-w-[720px] rounded-[22px] bg-white/55 px-20 py-12 shadow-sm">
        <div className="mx-auto grid h-16 max-w-[560px] place-items-center rounded-full border-[6px] border-slate-100 bg-blue-600 px-8 text-center text-xl font-bold text-white shadow-[0_2px_10px_rgba(71,85,105,0.35)]">
          Система контролю процесу дипломування
        </div>

        <form onSubmit={handleSubmit(submit)} className="mx-auto mt-16 max-w-[470px] text-center">
          <label className="block text-base font-medium text-slate-500" htmlFor="secretaryEmail">
            Введіть електронну пошту
          </label>
          <input
            id="secretaryEmail"
            type="email"
            placeholder="admin@gmail.com"
            autoComplete="email"
            className="mt-3 h-10 w-full rounded-xl border border-slate-300 bg-transparent px-5 text-center text-slate-600 outline-none transition placeholder:text-slate-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
            {...register('secretaryEmail')}
          />
          <div className="mt-2 min-h-6 text-sm font-semibold text-red-500">
            {errors.secretaryEmail?.message}
          </div>
          <button
            type="submit"
            className="mt-8 h-16 w-[345px] max-w-full rounded-full bg-white text-xl font-bold text-slate-600 shadow-sm transition hover:text-blue-600 focus:outline-none focus:ring-4 focus:ring-blue-100"
          >
            Увійти в систему
          </button>
        </form>
      </section>
    </main>
  )
}
