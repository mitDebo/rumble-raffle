import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

// Merges Tailwind classes, resolving conflicts (e.g. "p-2 p-4" -> "p-4")
// the way tailwind-merge understands, after clsx handles conditional
// classnames. Standard shadcn/ui helper — every generated component
// imports this from "@/lib/utils".
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
