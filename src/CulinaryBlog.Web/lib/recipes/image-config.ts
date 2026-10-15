export interface RecipeImageRemotePattern {
  protocol: "http" | "https";
  hostname: string;
  port: string;
  pathname: string;
}

function endpointPattern(endpoint: string, name: string): RecipeImageRemotePattern {
  let url: URL;
  try {
    url = new URL(endpoint);
  } catch {
    throw new TypeError(`${name} must be an absolute HTTP(S) URL.`);
  }
  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new TypeError(`${name} must use HTTP or HTTPS.`);
  }
  if (url.username || url.password || url.search || url.hash || url.hostname.includes("*")) {
    throw new TypeError(`${name} must identify one exact remote image origin without credentials, wildcards, query, or hash.`);
  }
  const basePath = url.pathname.replace(/\/+$/, "");
  return {
    protocol: url.protocol.slice(0, -1) as "http" | "https",
    hostname: url.hostname,
    port: url.port,
    pathname: basePath ? `${basePath}/**` : "/**",
  };
}

export function createRecipeImageRemotePatterns({
  cdnEndpoint,
  minioEndpoint = "http://localhost:9000",
}: {
  cdnEndpoint?: string;
  minioEndpoint?: string;
} = {}): RecipeImageRemotePattern[] {
  const endpoints = [
    { value: minioEndpoint.trim(), name: "MINIO_PUBLIC_ENDPOINT" },
    { value: cdnEndpoint?.trim(), name: "RECIPE_IMAGE_CDN_URL" },
  ].filter((endpoint): endpoint is { value: string; name: string } => Boolean(endpoint.value));

  const patterns = endpoints.map((endpoint) => endpointPattern(endpoint.value, endpoint.name));
  return patterns.filter((pattern, index) => patterns.findIndex((candidate) => (
    candidate.protocol === pattern.protocol
    && candidate.hostname === pattern.hostname
    && candidate.port === pattern.port
    && candidate.pathname === pattern.pathname
  )) === index);
}
