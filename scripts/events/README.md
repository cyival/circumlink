# Events

This directory contains the event hub and event types.

To be known that if you didn't unsubscribe from an event, it will stay subscribed even after the handler is removed, this will cause memory leaks.

So we provided a way to unsubscribe from events automatically when the handler is removed, using the Node extensions.
