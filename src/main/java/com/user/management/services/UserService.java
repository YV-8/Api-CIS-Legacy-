package com.user.management.services;

import java.util.List;

import org.modelmapper.ModelMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;

import com.user.management.dtos.UserRequestDTO;
import com.user.management.dtos.UserResponseDTO;
import com.user.management.exceptions.UserNotFoundException;
import com.user.management.models.User;
import com.user.management.repository.UserRepository;

@Service
public class UserService {

    @Autowired
    private ModelMapper modelMapper;

    @Autowired
    private UserRepository userRepository;

    public UserResponseDTO saveUser(UserRequestDTO user) {
        User userEntity = modelMapper.map(user, User.class);
        User savedUser = userRepository.save(userEntity);
        return modelMapper.map(savedUser, UserResponseDTO.class);
    }

    public List<UserResponseDTO> getAllUsers() {
        return userRepository.findAll()
                .stream()
                .map(user -> modelMapper.map(user, UserResponseDTO.class))
                .toList();
    }

    public UserResponseDTO updateUser(String id, UserRequestDTO request) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User not found with id: " + id));

        if (request.getLogin() != null
                && !request.getLogin().isBlank()
                && userRepository.existsByLoginAndIdNot(request.getLogin(), id)) {
            throw new DataIntegrityViolationException("Login already exists");
        }

        if (request.getName() != null && !request.getName().isBlank()) {
            user.setName(request.getName());
        }

        if (request.getLogin() != null && !request.getLogin().isBlank()) {
            user.setLogin(request.getLogin());
        }

        if (request.getPassword() != null && !request.getPassword().isBlank()) {
            user.setPassword(request.getPassword());
        }

        User updatedUser = userRepository.save(user);
        return modelMapper.map(updatedUser, UserResponseDTO.class);
    }

    public UserResponseDTO getUserById(String id) {
        User user = userRepository.findById(id)
                .orElseThrow(() -> new UserNotFoundException("User with id " + id + " was not found"));
        return modelMapper.map(user, UserResponseDTO.class);
    }
}