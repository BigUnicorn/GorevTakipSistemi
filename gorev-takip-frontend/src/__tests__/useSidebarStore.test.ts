import { describe, it, expect, beforeEach } from 'vitest';
import { useSidebarStore } from '../store/useSidebarStore';

describe('useSidebarStore', () => {
  const initialState = useSidebarStore.getState();

  beforeEach(() => {
    // Reset store before each test
    useSidebarStore.setState(initialState, true);
  });

  it('should have initial state with isOpen false', () => {
    const { isOpen } = useSidebarStore.getState();
    expect(isOpen).toBe(false);
  });

  it('should toggle isOpen state', () => {
    const store = useSidebarStore.getState();
    
    store.toggle();
    expect(useSidebarStore.getState().isOpen).toBe(true);

    useSidebarStore.getState().toggle();
    expect(useSidebarStore.getState().isOpen).toBe(false);
  });

  it('should close sidebar regardless of current state', () => {
    // Open it first
    useSidebarStore.getState().toggle();
    expect(useSidebarStore.getState().isOpen).toBe(true);

    // Close it
    useSidebarStore.getState().close();
    expect(useSidebarStore.getState().isOpen).toBe(false);

    // Close it again when already closed
    useSidebarStore.getState().close();
    expect(useSidebarStore.getState().isOpen).toBe(false);
  });
});
