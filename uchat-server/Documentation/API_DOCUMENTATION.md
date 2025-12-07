# UChat API Documentation

## Подключение

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/chat")
    .withAutomaticReconnect()
    .build();

await connection.start();
```

## Endpoints

### 1. Register
**Регистрация нового пользователя**

```javascript
const result = await connection.invoke("Register", username, password, deviceInfo, ipAddress);
```

**Параметры:**
- `username` (string, required) - имя пользователя
- `password` (string, required) - пароль
- `deviceInfo` (string, optional) - информация об устройстве (например: "Chrome/Windows")
- `ipAddress` (string, optional) - IP адрес (автоматически определяется если null)

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    refreshToken: string  // JWT токен (30 дней)
  }
}
```

---

### 2. Login
**Вход существующего пользователя**

```javascript
const result = await connection.invoke("Login", username, password, deviceInfo, ipAddress);
```

**Параметры:**
- `username` (string, required) - имя пользователя
- `password` (string, required) - пароль
- `deviceInfo` (string, optional) - информация об устройстве
- `ipAddress` (string, optional) - IP адрес

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    refreshToken: string  // JWT токен (30 дней)
  }
}
```

---

### 3. LoginWithRefreshToken
**Silent login - обновление refresh token**

```javascript
const result = await connection.invoke("LoginWithRefreshToken", refreshToken, deviceInfo, ipAddress);
```

**Параметры:**
- `refreshToken` (string, required) - refresh токен
- `deviceInfo` (string, optional) - обновить информацию об устройстве
- `ipAddress` (string, optional) - обновить IP адрес

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: {
    refreshToken: string   // новый refresh токен
  }
}
```

**Важно:** Старый refresh токен становится недействительным!

---

### 4. GetActiveSessions
**Получить список активных сессий пользователя**

```javascript
const result = await connection.invoke("GetActiveSessions", refreshToken);
```

**Параметры:**
- `refreshToken` (string, required) - refresh токен

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: [
    {
      token: string,           // refresh токен сессии
      deviceInfo: string,      // информация об устройстве
      createdAt: DateTime,     // дата создания
      lastActivityAt: DateTime,// последняя активность
      expiresAt: DateTime      // дата истечения
    }
  ]
}
```

---

### 5. RevokeSession
**Отозвать конкретную сессию**

```javascript
const result = await connection.invoke("RevokeSession", refreshToken, sessionId);
```

**Параметры:**
- `refreshToken` (string, required) - refresh токен
- `sessionId` (int, required) - ID сессии для отзыва

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: boolean  // true если успешно
}
```

---

### 6. Logout
**Выход - отзыв текущей сессии**

```javascript
const result = await connection.invoke("Logout", refreshToken);
```

**Параметры:**
- `refreshToken` (string, required) - refresh токен

**Возвращает:**
```typescript
{
  success: boolean,
  message: string,
  data: boolean  // true если успешно
}
```

**Важно:** После logout refresh токен становится недействительным!

---

### 7. SendMessage
**Отправить сообщение в чат**

```javascript
await connection.invoke("SendMessage", refreshToken, message);
```

**Параметры:**
- `refreshToken` (string, required) - refresh токен
- `message` (string, required) - текст сообщения

**Возвращает:** void (ничего)

**События:**
- При успехе отправляет всем в группе "LoggedIn" событие `ReceiveMessage`
- При ошибке отправляет отправителю событие `Error`

---

## События SignalR

### ReceiveMessage
**Получение нового сообщения**

```javascript
connection.on("ReceiveMessage", (message) => {
  console.log(message);
  // {
  //   connectionId: string,
  //   username: string,
  //   content: string,
  //   sentAt: DateTime
  // }
});
```

### Error
**Получение ошибки**

```javascript
connection.on("Error", (errorMessage) => {
  console.error(errorMessage);
});
```

---

## Типичные сценарии

### Первичная авторизация
```javascript
// 1. Регистрация или логин
const authResult = await connection.invoke("Register", "user123", "pass123", "Chrome/Windows");
// или
const authResult = await connection.invoke("Login", "user123", "pass123", "Chrome/Windows");

// 2. Сохранить токен
localStorage.setItem('refreshToken', authResult.data.refreshToken);
```

### Автоматический вход (Silent Login)
```javascript
const refreshToken = localStorage.getItem('refreshToken');
const result = await connection.invoke("LoginWithRefreshToken", refreshToken);

if (result.success) {
  localStorage.setItem('refreshToken', result.data.refreshToken);
} else {
  // Перенаправить на страницу логина
}
```

### Отправка сообщения
```javascript
const refreshToken = localStorage.getItem('refreshToken');
await connection.invoke("SendMessage", refreshToken, "Hello, World!");
```

### Выход
```javascript
const refreshToken = localStorage.getItem('refreshToken');
await connection.invoke("Logout", refreshToken);

localStorage.removeItem('refreshToken');
```

---

## Коды ошибок

Все методы возвращают объект с полем `success`:
- `true` - операция успешна
- `false` - произошла ошибка (см. поле `message`)

**Типичные ошибки:**
- `"Invalid or expired refresh token"` - refresh токен недействителен или истек
- `"User not found or invalid password"` - неверные учетные данные
- `"Username already exists"` - пользователь уже существует
- `"Session expired or not found"` - сессия истекла или удалена
- `"Internal server error"` - внутренняя ошибка сервера

---

## Время жизни токена

- **Refresh Token**: 30 дней (2592000000 ms)

**Рекомендации:**
- Используйте `LoginWithRefreshToken` периодически для продления сессии
- Используйте `LoginWithRefreshToken` при запуске приложения
- Храните refresh token безопасно (HttpOnly cookie или secure storage)
