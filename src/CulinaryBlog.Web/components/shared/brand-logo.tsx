import Image from "next/image";

/** The conical-hat-over-pho-bowl mark. Decorative: every use sits next to the "Culinary Blog" wordmark. */
export function BrandLogo({ size }: { size: number }) {
  return <Image src="/images/brand/logo-mark.png" alt="" width={size} height={size} className="brand-logo" priority />;
}
