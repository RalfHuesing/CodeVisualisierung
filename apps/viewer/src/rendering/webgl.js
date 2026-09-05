export function isWebGLSupported(documentObject) {
  const canvas = documentObject?.createElement?.("canvas");
  if (!canvas?.getContext) {
    return false;
  }

  return Boolean(canvas.getContext("webgl2") ?? canvas.getContext("webgl"));
}
