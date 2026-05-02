package com.user.management.services;

import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.dtos.UserUpdateRequestDTO;
import com.user.management.enums.Role;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.models.User;
import com.user.management.repository.UserRepositoryPort;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.modelmapper.ModelMapper;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.crypto.password.PasswordEncoder;

import java.util.List;
import java.util.Optional;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
class UserServiceTest {

    @Mock
    private ModelMapper modelMapper;

    @Mock
    private UserRepositoryPort userRepository;

    @Mock
    private PasswordEncoder passwordEncoder;

    @InjectMocks
    private UserService userService;

    private UserRequestDTO request;
    private UserUpdateRequestDTO updateRequest;

    @BeforeEach
    void setUp() {
        request = new UserRequestDTO();
        request.setName("David");
        request.setLogin("david");
        request.setPassword("1234");
        request.setRole("user");

        updateRequest = new UserUpdateRequestDTO();
        updateRequest.setName("David Updated");
        updateRequest.setLogin("david2");
        updateRequest.setPassword("5678");
        updateRequest.setRole("admin");
    }

    @Test
    void saveUser_shouldReturnSavedUser() {
        User userEntity = new User(null, "David", "david", "1234", Role.USER);
        User savedUser = new User("1", "David", "david", "encoded1234", Role.USER);
        UserResponseDTO response = new UserResponseDTO("1", "David", "david", "USER");

        when(modelMapper.map(request, User.class)).thenReturn(userEntity);
        when(passwordEncoder.encode("1234")).thenReturn("encoded1234");
        when(userRepository.save(userEntity)).thenReturn(savedUser);
        when(modelMapper.map(savedUser, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.saveUser(request);

        assertNotNull(result);
        assertEquals("1", result.getId());
        assertEquals("David", result.getName());
        assertEquals("david", result.getLogin());
        assertEquals("USER", result.getRole());

        verify(userRepository, times(1)).save(userEntity);
    }

    @Test
    void getAllUsers_shouldReturnUsersList() {
        User user = new User("1", "David", "david", "1234", Role.USER);
        UserResponseDTO response = new UserResponseDTO("1", "David", "david", "USER");

        when(userRepository.findAll()).thenReturn(List.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(response);

        List<UserResponseDTO> result = userService.getAllUsers();

        assertEquals(1, result.size());
        assertEquals("1", result.get(0).getId());
        assertEquals("David", result.get(0).getName());
        assertEquals("david", result.get(0).getLogin());
        assertEquals("USER", result.get(0).getRole());
    }

    @Test
    void getUserById_shouldReturnUser() {
        User user = new User("1", "David", "david", "1234", Role.USER);
        UserResponseDTO response = new UserResponseDTO("1", "David", "david", "USER");

        when(userRepository.findById("1")).thenReturn(Optional.of(user));
        when(modelMapper.map(user, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.getUserById("1");

        assertEquals("1", result.getId());
        assertEquals("David", result.getName());
        assertEquals("david", result.getLogin());
        assertEquals("USER", result.getRole());
    }

    @Test
    void getUserById_notFound_shouldThrow() {
        when(userRepository.findById("99")).thenReturn(Optional.empty());

        assertThrows(UserNotFoundException.class, () -> userService.getUserById("99"));
    }

    @Test
    void updateUser_shouldUpdateExistingUser() {
        User existingUser = new User("1", "David", "david", "1234", Role.USER);
        User savedUser = new User("1", "David Updated", "david2", "encoded5678", Role.ADMIN);
        UserResponseDTO response = new UserResponseDTO("1", "David Updated", "david2", "ADMIN");

        when(userRepository.findById("1")).thenReturn(Optional.of(existingUser));
        when(passwordEncoder.encode("5678")).thenReturn("encoded5678");
        when(userRepository.save(existingUser)).thenReturn(savedUser);
        when(modelMapper.map(savedUser, UserResponseDTO.class)).thenReturn(response);

        UserResponseDTO result = userService.updateUser("1", updateRequest);

        assertEquals("David Updated", result.getName());
        assertEquals("david2", result.getLogin());
        assertEquals("ADMIN", result.getRole());
        verify(userRepository, times(1)).save(existingUser);
    }

    @Test
    void updateUser_whenNotFound_shouldThrow() {
        when(userRepository.findById("99")).thenReturn(Optional.empty());

        assertThrows(UserNotFoundException.class, () -> userService.updateUser("99", updateRequest));
    }

    @Test
    void deleteUserById_shouldDeleteUser() {
        when(userRepository.existsById("1")).thenReturn(true);

        assertDoesNotThrow(() -> userService.deleteUserById("1"));
        verify(userRepository, times(1)).deleteById("1");
    }

    @Test
    void deleteUserById_notFound_shouldThrow() {
        when(userRepository.existsById("99")).thenReturn(false);

        assertThrows(UserNotFoundException.class, () -> userService.deleteUserById("99"));
    }

    @Test
    void saveUser_whenLoginAlreadyExists_shouldThrow() {
        User userEntity = new User(null, "David", "david", "1234", Role.USER);

        when(modelMapper.map(request, User.class)).thenReturn(userEntity);
        when(passwordEncoder.encode("1234")).thenReturn("encoded1234");
        when(userRepository.save(userEntity)).thenThrow(new DataIntegrityViolationException("Login already exists"));

        assertThrows(DataIntegrityViolationException.class, () -> userService.saveUser(request));
    }

    @Test
    void loadUserByUsername_existingUser_shouldReturnUserDetails() {
        User user = new User("1", "Alice", "alice", "encodedPassword", Role.USER);
        when(userRepository.findByLogin("alice")).thenReturn(Optional.of(user));

        UserDetails result = userService.loadUserByUsername("alice");

        assertNotNull(result);
        assertEquals("alice", result.getUsername());
        assertEquals("encodedPassword", result.getPassword());
        assertTrue(result.getAuthorities().stream()
                .anyMatch(a -> a.getAuthority().equals("ROLE_USER")));
    }

    @Test
    void loadUserByUsername_notFound_shouldThrowUsernameNotFoundException() {
        when(userRepository.findByLogin("unknown")).thenReturn(Optional.empty());

        assertThrows(UsernameNotFoundException.class,
                () -> userService.loadUserByUsername("unknown"));
    }
}
