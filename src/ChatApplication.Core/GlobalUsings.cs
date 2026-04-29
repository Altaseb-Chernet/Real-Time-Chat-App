// Disambiguate our User model from the System.Security.Principal namespace
// that the compiler resolves when it sees the 'User' identifier in this project.
global using User = ChatApplication.Core.Modules.User.Models.User;
