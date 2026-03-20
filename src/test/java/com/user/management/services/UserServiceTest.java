package com.user.management.services;

import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;
import com.user.management.exceptions.UserNotFoundException;
import org.springframework.dao.DataIntegrityViolationException;
import static org.junit.jupiter.api.Assertions.assertThrows;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.modelmapper.ModelMapper;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class UserServiceTest {

    @Mock
    private ModelMapper modelMapper;

    @Mock
    private UserRepository userRepository;

    @InjectMocks
    private UserService userService;

    @Test
    void saveUser_shouldReturnSavedUser() {
        UserRequestDTO request = new UserRequestDTO();
        request.setName("David");
        request.setLogin("david");
        request.setPassword("1234");

        User userEntity = new User(null, "David", "david", "1234");
        User savedUser = new User("1", "David", "david", "1234");
        UserResponseDTO response = new UserResponseDTO("1", "David", "david");

        when(modelMapper.map(request, User.class)).thenReturn(userEntity);
        when(userRepository.save(userEntity)).thenReturn(savedUser);
        when(modelMapper.map(savedUser, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.saveUser(request);

        assertNotNull(result);
        assertEquals("1", result.getId());
        assertEquals("David", result.getName());
        assertEquals("david", result.getLogin());

        verify(userRepository).save(userEntity);
    }

    @Test
    void getAllUsers_shouldReturnUsersList() {
        User user = new User("1", "David", "david", "1234");
        UserResponseDTO response = new UserResponseDTO("1", "David", "david");

        when(userRepository.findAll()).thenReturn(List.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(response);

        List<UserResponseDTO> result = userService.getAllUsers();

        assertEquals(1, result.size());
        assertEquals("David", result.get(0).getName());
        assertEquals("david", result.get(0).getLogin());
    }

    @Test
    void getUserById_shouldReturnUser() {
        User user = new User("1", "David", "david", "1234");
        UserResponseDTO response = new UserResponseDTO("1", "David", "david");

        when(userRepository.findById("1")).thenReturn(Optional.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.getUserById("1");

        assertEquals("1", result.getId());
        assertEquals("David", result.getName());
        assertEquals("david", result.getLogin());
    }

    @Test
    void updateUser_shouldUpdateExistingUser() {
        User existingUser = new User("1", "David", "david", "1234");

        UserRequestDTO request = new UserRequestDTO();
        request.setName("David Updated");
        request.setLogin("david2");
        request.setPassword("5678");

        User updatedUser = new User("1", "David Updated", "david2", "5678");
        UserResponseDTO response = new UserResponseDTO("1", "David Updated", "david2");

        when(userRepository.findById("1")).thenReturn(Optional.of(existingUser));
        when(userRepository.existsByLoginAndIdNot("david2", "1")).thenReturn(false);
        when(userRepository.save(existingUser)).thenReturn(updatedUser);
        when(modelMapper.map(updatedUser, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.updateUser("1", request);

        assertEquals("David Updated", result.getName());
        assertEquals("david2", result.getLogin());
    }

    @Test
    void deleteUserById_shouldDeleteUser() {
        when(userRepository.existsById("1")).thenReturn(true);
        doNothing().when(userRepository).deleteById("1");

        userService.deleteUserById("1");

        verify(userRepository).deleteById("1");
    }

    @Test
    void saveUser_shouldThrowExceptionWhenLoginAlreadyExists() {
    UserRequestDTO request = new UserRequestDTO();
    request.setName("David");
    request.setLogin("david");
    request.setPassword("1234");

    User userEntity = new User(null, "David", "david", "1234");

    when(modelMapper.map(request, User.class)).thenReturn(userEntity);
    when(userRepository.save(userEntity))
            .thenThrow(new DataIntegrityViolationException("Login already exists"));

    assertThrows(DataIntegrityViolationException.class, () -> userService.saveUser(request));
}

@Test
void getUserById_shouldThrowExceptionWhenUserDoesNotExist() {
    when(userRepository.findById("99")).thenReturn(Optional.empty());

    assertThrows(UserNotFoundException.class, () -> userService.getUserById("99"));
}

    @Test
    void updateUser_shouldThrowExceptionWhenUserDoesNotExist() {
    UserRequestDTO request = new UserRequestDTO();
    request.setName("David Updated");
    request.setLogin("david2");
    request.setPassword("5678");

    when(userRepository.findById("99")).thenReturn(Optional.empty());

    assertThrows(UserNotFoundException.class, () -> userService.updateUser("99", request));
}

    @Test
    void deleteUserById_shouldThrowExceptionWhenUserAlreadyDeleted() {
    when(userRepository.existsById("99")).thenReturn(false);

    assertThrows(UserNotFoundException.class, () -> userService.deleteUserById("99"));
}
}