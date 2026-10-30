import assert from "node:assert/strict";
import test, { mock } from "node:test";
import { createToastStore } from "../../lib/ui/toast-store.ts";

test("a pushed toast is visible and expires after the duration", () => {
  mock.timers.enable({ apis: ["setTimeout"] });
  try {
    const store = createToastStore({ durationMs: 5000, maxVisible: 3 });
    store.push("success", "Đã lưu");

    assert.deepEqual(store.getSnapshot().map((t) => t.message), ["Đã lưu"]);
    mock.timers.tick(4999);
    assert.equal(store.getSnapshot().length, 1);
    mock.timers.tick(1);
    assert.equal(store.getSnapshot().length, 0);
  } finally {
    mock.timers.reset();
  }
});

test("at most three toasts stay visible and the oldest is dropped first", () => {
  const store = createToastStore({ durationMs: 5000, maxVisible: 3 });
  for (const message of ["1", "2", "3", "4"]) store.push("success", message);

  assert.deepEqual(store.getSnapshot().map((t) => t.message), ["2", "3", "4"]);
});

test("error toasts announce assertively and success toasts politely", () => {
  const store = createToastStore({ durationMs: 5000, maxVisible: 3 });
  store.push("error", "Lỗi");
  store.push("success", "Xong");

  assert.deepEqual(store.getSnapshot().map((t) => t.role), ["alert", "status"]);
});

test("dismiss removes one toast and subscribers are notified", () => {
  const store = createToastStore({ durationMs: 5000, maxVisible: 3 });
  let notifications = 0;
  const unsubscribe = store.subscribe(() => { notifications += 1; });
  const id = store.push("success", "A");
  store.dismiss(id);
  unsubscribe();
  store.push("success", "B");

  assert.equal(store.getSnapshot().length, 1);
  assert.equal(notifications, 2);
});
