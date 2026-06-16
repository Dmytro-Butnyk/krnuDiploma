import axios from 'axios'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'https://localhost:7170',
  headers: {
    Accept: 'application/json',
  },
})
